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

private func captionLine(
    _ text: String, size: CGFloat, weight: NSFont.Weight, color ink: UInt32, width: CGFloat
) -> CTLine? {
    func makeLine(_ text: String, at fontSize: CGFloat) -> CTLine {
        CTLineCreateWithAttributedString(NSAttributedString(string: text, attributes: [
            NSAttributedString.Key(kCTFontAttributeName as String): NSFont.systemFont(ofSize: fontSize, weight: weight),
            NSAttributedString.Key(kCTForegroundColorAttributeName as String): color(ink)
        ]))
    }

    var fontSize = size
    var line = makeLine(text, at: fontSize)
    let availableWidth = width - 4
    guard availableWidth > 0 else { return nil }
    // Keep each row single-line, including on narrower recordings.
    while CTLineGetTypographicBounds(line, nil, nil, nil) > availableWidth && fontSize > 11 {
        fontSize -= 1
        line = makeLine(text, at: fontSize)
    }
    if CTLineGetTypographicBounds(line, nil, nil, nil) > availableWidth {
        return CTLineCreateTruncatedLine(line, availableWidth, .end, makeLine("…", at: fontSize))
    }
    return line
}

private func drawLine(_ line: CTLine, in context: CGContext, x: CGFloat, centerY: CGFloat) {
    var ascent: CGFloat = 0
    var descent: CGFloat = 0
    _ = CTLineGetTypographicBounds(line, &ascent, &descent, nil)
    context.textPosition = CGPoint(x: x, y: round(centerY - (ascent - descent) / 2))
    CTLineDraw(line, context)
}

private func captionImage(_ caption: Caption, width: CGFloat) throws -> CGImage {
    guard let context = CGContext(
        data: nil, width: Int(width), height: Int(stripHeight), bitsPerComponent: 8, bytesPerRow: 0,
        space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue
    ) else {
        throw AnnotationError("Could not create the caption bitmap context.")
    }
    context.clear(CGRect(x: 0, y: 0, width: width, height: stripHeight))
    context.setAllowsAntialiasing(true)
    context.setShouldAntialias(true)
    context.setShouldSmoothFonts(false)
    context.textMatrix = .identity

    let inset = min(26, floor(width * 0.04))
    let tag = captionLine(caption.tag, size: 19, weight: .semibold, color: 0xF8D68F, width: width * 0.2)
    let tagWidth = tag.map { CTLineGetTypographicBounds($0, nil, nil, nil) } ?? 0
    let badge = CGRect(x: inset, y: 52, width: ceil(tagWidth) + 24, height: 30)
    context.setFillColor(color(0x2E302A))
    context.addPath(CGPath(roundedRect: badge, cornerWidth: 6, cornerHeight: 6, transform: nil))
    context.fillPath()
    if let tag {
        drawLine(tag, in: context, x: badge.midX - tagWidth / 2, centerY: badge.midY)
    }

    let titleX = badge.maxX + 14
    if let title = captionLine(caption.title, size: 28, weight: .bold, color: 0xF2F7FA, width: width - titleX - inset) {
        drawLine(title, in: context, x: titleX, centerY: badge.midY)
    }
    if let subtitle = captionLine(caption.subtitle, size: 23, weight: .regular, color: 0xCADBE4, width: width - inset * 2) {
        drawLine(subtitle, in: context, x: inset, centerY: 31)
    }

    guard let image = context.makeImage() else {
        throw AnnotationError("Could not create the caption image.")
    }
    return image
}

private func captionStrip(width: CGFloat, duration: Double, captions: [Caption]) throws -> CALayer {
    let strip = solidLayer(CGRect(x: 0, y: 0, width: width, height: stripHeight), color: 0x101D28)
    strip.masksToBounds = true
    strip.addSublayer(solidLayer(CGRect(x: 0, y: stripHeight - 2, width: width, height: 2), color: 0x3F998E))
    strip.addSublayer(solidLayer(CGRect(x: 0, y: stripHeight - 2, width: min(72, width), height: 2), color: 0xE3BA68))

    for caption in captions where caption.start < duration {
        let phase = CALayer()
        phase.frame = strip.bounds
        phase.opacity = caption.start == 0 ? 1 : 0
        phase.contents = try captionImage(caption, width: width)

        // Discrete opacity changes put each phase on [start, end) in export time.
        var times: [NSNumber] = [0]
        var opacities: [NSNumber] = [NSNumber(value: phase.opacity)]
        if caption.start > 0 {
            times.append(NSNumber(value: caption.start / duration))
            opacities.append(1.0)
        }
        if caption.end < duration {
            times.append(NSNumber(value: caption.end / duration))
            opacities.append(0.0)
        }
        times.append(1)
        opacities.append(caption.end < duration ? 0.0 : 1.0)

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
    canvas.addSublayer(try captionStrip(width: renderSize.width, duration: duration, captions: captions))

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
