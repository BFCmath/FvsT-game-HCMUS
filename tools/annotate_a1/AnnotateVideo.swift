import AppKit
import AVFoundation
import CoreText
import QuartzCore

private let stripHeight: CGFloat = 96

private struct AnnotationError: LocalizedError {
    let message: String
    var errorDescription: String? { message }

    init(_ message: String) {
        self.message = message
    }
}

private struct Caption: Decodable {
    let start: Double
    let end: Double
    let tag: String
    let title: String
    let subtitle: String

    static func load(from url: URL) throws -> [Caption] {
        let captions = try JSONDecoder().decode([Caption].self, from: Data(contentsOf: url))
        guard !captions.isEmpty else {
            throw AnnotationError("The caption timeline must contain at least one phase.")
        }

        var previousEnd = 0.0
        for (index, caption) in captions.enumerated() {
            let phase = "Caption \(index + 1)"
            guard caption.start.isFinite, caption.end.isFinite,
                  caption.start >= previousEnd, caption.end > caption.start else {
                throw AnnotationError("\(phase) must have a nonnegative start and an end after its start. List phases in order and avoid overlaps.")
            }
            for field in [caption.tag, caption.title, caption.subtitle] {
                guard !field.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty,
                      field.rangeOfCharacter(from: .newlines) == nil else {
                    throw AnnotationError("\(phase) needs a nonempty, single-line tag, title, and subtitle.")
                }
            }
            previousEnd = caption.end
        }
        return captions
    }
}

private func color(_ hex: UInt32) -> CGColor {
    NSColor(
        srgbRed: CGFloat((hex >> 16) & 255) / 255,
        green: CGFloat((hex >> 8) & 255) / 255,
        blue: CGFloat(hex & 255) / 255,
        alpha: 1
    ).cgColor
}

private func solidLayer(_ frame: CGRect, color fill: UInt32) -> CALayer {
    let layer = CALayer()
    layer.frame = frame
    layer.backgroundColor = color(fill)
    return layer
}

private func textLayer(
    _ text: String, size: CGFloat, weight: NSFont.Weight, color ink: UInt32, width: CGFloat
) -> CATextLayer {
    func attributedText(at fontSize: CGFloat) -> NSAttributedString {
        NSAttributedString(string: text, attributes: [
            NSAttributedString.Key(kCTFontAttributeName as String): NSFont.systemFont(ofSize: fontSize, weight: weight),
            NSAttributedString.Key(kCTForegroundColorAttributeName as String): color(ink)
        ])
    }

    var fontSize = size
    var attributed = attributedText(at: size)
    var line = CTLineCreateWithAttributedString(attributed)
    var lineWidth = CGFloat(CTLineGetTypographicBounds(line, nil, nil, nil))
    // Keep each row single-line, including on narrower recordings.
    while ceil(lineWidth) + 4 > width && fontSize > 11 {
        fontSize -= 1
        attributed = attributedText(at: fontSize)
        line = CTLineCreateWithAttributedString(attributed)
        lineWidth = CGFloat(CTLineGetTypographicBounds(line, nil, nil, nil))
    }

    var ascent: CGFloat = 0
    var descent: CGFloat = 0
    var leading: CGFloat = 0
    _ = CTLineGetTypographicBounds(line, &ascent, &descent, &leading)

    let layer = CATextLayer()
    layer.string = attributed
    layer.contentsScale = 2
    layer.truncationMode = .end
    layer.bounds.size = CGSize(
        width: min(ceil(lineWidth) + 4, width),
        height: ceil(ascent + descent + max(0, leading)) + 2
    )
    return layer
}

private func captionStrip(width: CGFloat, duration: Double, captions: [Caption]) -> CALayer {
    let strip = solidLayer(CGRect(x: 0, y: 0, width: width, height: stripHeight), color: 0x101D28)
    strip.masksToBounds = true
    strip.addSublayer(solidLayer(CGRect(x: 0, y: stripHeight - 2, width: width, height: 2), color: 0x3F998E))
    strip.addSublayer(solidLayer(CGRect(x: 0, y: stripHeight - 2, width: min(72, width), height: 2), color: 0xE3BA68))

    let inset = min(26, floor(width * 0.04))
    for caption in captions where caption.start < duration {
        let phase = CALayer()
        phase.frame = strip.bounds
        phase.opacity = caption.start == 0 ? 1 : 0

        let tag = textLayer(caption.tag, size: 19, weight: .semibold, color: 0xF8D68F, width: width * 0.2)
        let badge = solidLayer(
            CGRect(x: inset, y: 52, width: ceil(tag.bounds.width) + 20, height: 30),
            color: 0x2E302A
        )
        badge.cornerRadius = 6
        tag.alignmentMode = .center
        tag.position = CGPoint(x: badge.bounds.midX, y: badge.bounds.midY)
        badge.addSublayer(tag)
        phase.addSublayer(badge)

        let titleX = badge.frame.maxX + 14
        let title = textLayer(caption.title, size: 28, weight: .bold, color: 0xF2F7FA, width: width - titleX - inset)
        title.frame.origin = CGPoint(x: titleX, y: floor(badge.frame.midY - title.bounds.height / 2))
        phase.addSublayer(title)

        let subtitle = textLayer(caption.subtitle, size: 23, weight: .regular, color: 0xCADBE4, width: width - inset * 2)
        subtitle.frame.origin = CGPoint(x: inset, y: floor(31 - subtitle.bounds.height / 2))
        phase.addSublayer(subtitle)

        // Discrete opacity changes put each phase on [start, end) in export time.
        var times: [NSNumber] = [0]
        var opacities = [caption.start == 0 ? 1 : 0]
        if caption.start > 0 {
            times.append(NSNumber(value: caption.start / duration))
            opacities.append(1)
        }
        if caption.end < duration {
            times.append(NSNumber(value: caption.end / duration))
            opacities.append(0)
        }
        times.append(1)
        opacities.append(caption.end < duration ? 0 : 1)

        let visibility = CAKeyframeAnimation(keyPath: "opacity")
        visibility.beginTime = AVCoreAnimationBeginTimeAtZero
        visibility.duration = duration
        visibility.calculationMode = .discrete
        visibility.keyTimes = times
        visibility.values = opacities
        visibility.isRemovedOnCompletion = false
        visibility.fillMode = .both
        phase.add(visibility, forKey: "captionVisibility")
        strip.addSublayer(phase)
    }
    return strip
}

private func annotate(input: URL, config: URL, output: URL) async throws {
    guard ["mov", "mp4"].contains(input.pathExtension.lowercased()),
          output.pathExtension.lowercased() == "mp4" else {
        throw AnnotationError("Use a .mov or .mp4 input and an .mp4 output.")
    }

    let files = FileManager.default
    guard !files.fileExists(atPath: output.path) else {
        throw AnnotationError("Output already exists. Choose a new path: \(output.path)")
    }

    let captions = try Caption.load(from: config)
    let source = AVURLAsset(url: input)
    guard let video = try await source.loadTracks(withMediaType: .video).first else {
        throw AnnotationError("The input has no video track.")
    }

    let (naturalSize, orientation, range, frameRate) = try await video.load(
        .naturalSize, .preferredTransform, .timeRange, .nominalFrameRate
    )
    let duration = range.duration.seconds
    guard duration.isFinite, duration > 0 else {
        throw AnnotationError("The input video has no valid duration.")
    }
    guard captions[0].start < duration else {
        throw AnnotationError("The timeline starts after the last video frame. Check the caption timestamps.")
    }

    let orientedFrame = CGRect(origin: .zero, size: naturalSize).applying(orientation)
    func evenPixels(_ value: CGFloat) -> CGFloat { ceil(value / 2) * 2 }
    let renderSize = CGSize(
        width: evenPixels(orientedFrame.width),
        height: evenPixels(orientedFrame.height) + stripHeight
    )
    guard renderSize.width >= 96, orientedFrame.height > 0 else {
        throw AnnotationError("The input frame is too small for the caption strip.")
    }

    let composition = AVMutableComposition()
    guard let editor = composition.addMutableTrack(withMediaType: .video, preferredTrackID: kCMPersistentTrackID_Invalid) else {
        throw AnnotationError("Could not create the composition's video track.")
    }
    try editor.insertTimeRange(range, of: video, at: .zero)
    for audio in try await source.loadTracks(withMediaType: .audio) {
        let overlap = CMTimeRangeGetIntersection(range, otherRange: try await audio.load(.timeRange))
        guard overlap.isValid, overlap.duration > .zero else { continue }
        guard let track = composition.addMutableTrack(withMediaType: .audio, preferredTrackID: kCMPersistentTrackID_Invalid) else {
            throw AnnotationError("Could not create a composition audio track.")
        }
        try track.insertTimeRange(overlap, of: audio, at: overlap.start - range.start)
    }

    let editorInstruction = AVMutableVideoCompositionLayerInstruction(assetTrack: editor)
    // AVFoundation positions the oriented source at the top of the enlarged canvas.
    let topLeft = orientation.concatenating(
        CGAffineTransform(translationX: -orientedFrame.minX, y: -orientedFrame.minY)
    )
    editorInstruction.setTransform(topLeft, at: .zero)

    let instruction = AVMutableVideoCompositionInstruction()
    instruction.timeRange = CMTimeRange(start: .zero, duration: range.duration)
    instruction.layerInstructions = [editorInstruction]
    instruction.backgroundColor = color(0x000000)

    // Core Animation uses a bottom-left origin here. Give the strip its own clipped layer.
    let canvas = CALayer()
    canvas.frame = CGRect(origin: .zero, size: renderSize)
    let renderedVideo = CALayer()
    renderedVideo.frame = canvas.bounds
    canvas.addSublayer(renderedVideo)
    canvas.addSublayer(captionStrip(width: renderSize.width, duration: duration, captions: captions))

    let videoComposition = AVMutableVideoComposition()
    videoComposition.instructions = [instruction]
    videoComposition.renderSize = renderSize
    let fps = frameRate.isFinite && frameRate >= 1 ? Double(frameRate) : 30
    videoComposition.frameDuration = CMTime(seconds: 1 / fps, preferredTimescale: 60_000)
    videoComposition.animationTool = AVVideoCompositionCoreAnimationTool(
        postProcessingAsVideoLayer: renderedVideo, in: canvas
    )

    guard let exporter = AVAssetExportSession(asset: composition, presetName: AVAssetExportPresetHighestQuality),
          exporter.supportedFileTypes.contains(.mp4) else {
        throw AnnotationError("AVFoundation could not configure an MP4 export for this video.")
    }

    // Stage the render beside the destination so a failed export leaves no partial output.
    let staging = output.deletingLastPathComponent().appendingPathComponent(".annotate-a1-\(UUID().uuidString)")
    try files.createDirectory(at: staging, withIntermediateDirectories: false)
    defer { try? files.removeItem(at: staging) }

    let renderedURL = staging.appendingPathComponent("render.mp4")
    exporter.outputURL = renderedURL
    exporter.outputFileType = .mp4
    exporter.shouldOptimizeForNetworkUse = true
    exporter.videoComposition = videoComposition

    print("Exporting \(Int(renderSize.width)) × \(Int(renderSize.height)) to \(output.path)")
    await exporter.export()
    guard exporter.status == .completed else {
        throw exporter.error ?? AnnotationError("AVFoundation did not complete the export.")
    }

    let rendered = AVURLAsset(url: renderedURL)
    guard let renderedTrack = try await rendered.loadTracks(withMediaType: .video).first else {
        throw AnnotationError("The export has no video track.")
    }
    let (renderedSize, formats) = try await renderedTrack.load(.naturalSize, .formatDescriptions)
    guard renderedSize == renderSize,
          !formats.isEmpty,
          formats.allSatisfy({ CMFormatDescriptionGetMediaSubType($0) == kCMVideoCodecType_H264 }) else {
        throw AnnotationError("The export did not preserve the requested frame size or codec (H.264).")
    }

    try files.moveItem(at: renderedURL, to: output)
    print("Saved \(output.path)")
}

do {
    let paths = CommandLine.arguments.dropFirst().map { URL(fileURLWithPath: $0).standardizedFileURL }
    guard paths.count == 3 else {
        throw AnnotationError("Usage: annotate_video.sh <input.mov|input.mp4> <captions.json> <output.mp4>")
    }
    try await annotate(input: paths[0], config: paths[1], output: paths[2])
} catch {
    let failure = error as NSError
    var details = [failure.localizedDescription]
    if let reason = failure.localizedFailureReason {
        details.append(reason)
    }
    if let underlying = failure.userInfo[NSUnderlyingErrorKey] as? NSError {
        details.append("\(underlying.domain) \(underlying.code).")
    }
    FileHandle.standardError.write(Data("Annotate A1: \(details.joined(separator: " "))\n".utf8))
    exit(1)
}
