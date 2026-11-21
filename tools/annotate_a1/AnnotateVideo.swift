import AppKit
import AVFoundation
import CoreText
import QuartzCore

private struct AnnotationError: LocalizedError {
    let message: String
    var errorDescription: String? { message }

    init(_ message: String) {
        self.message = message
    }
}

private struct Callout: Decodable {
    let rect: [CGFloat]?
    let label: String?
    let labelPos: [CGFloat]?
    let arrowTo: [CGFloat]?

    func validate(in phase: String) throws {
        if let rect {
            guard rect.count == 4, normalized(rect),
                  rect[2] > 0, rect[3] > 0,
                  rect[0] + rect[2] <= 1, rect[1] + rect[3] <= 1 else {
                throw AnnotationError("\(phase) needs a rect [x, y, w, h] that fits inside the normalized frame.")
            }
        }
        guard rect != nil || label != nil else {
            throw AnnotationError("\(phase) needs a rect or a label on each callout.")
        }
        if let label {
            guard singleLine(label), let labelPos, pointIsValid(labelPos) else {
                throw AnnotationError("\(phase) needs a single-line label and a normalized labelPos [x, y].")
            }
        } else if labelPos != nil || arrowTo != nil {
            throw AnnotationError("\(phase) needs a label to use labelPos or arrowTo.")
        }
        if let arrowTo, !pointIsValid(arrowTo) {
            throw AnnotationError("\(phase) needs a normalized arrowTo [x, y].")
        }
    }
}

private struct Caption: Decodable {
    let start: Double
    let end: Double
    let tag: String
    let title: String
    let headerPos: [CGFloat]?
    let callouts: [Callout]

    static func load(from url: URL) throws -> [Caption] {
        let captions = try JSONDecoder().decode([Caption].self, from: Data(contentsOf: url))
        guard !captions.isEmpty else {
            throw AnnotationError("The caption timeline must contain at least one phase.")
        }

        var previousEnd = 0.0
        for (index, caption) in captions.enumerated() {
            let phase = "Phase \(index + 1)"
            guard caption.start.isFinite, caption.end.isFinite,
                  caption.start >= previousEnd, caption.end > caption.start else {
                throw AnnotationError("\(phase) must have a nonnegative start and an end after its start. List phases in order and avoid overlaps.")
            }
            guard singleLine(caption.tag), singleLine(caption.title) else {
                throw AnnotationError("\(phase) needs a nonempty, single-line tag and title.")
            }
            if let headerPos = caption.headerPos, !pointIsValid(headerPos) {
                throw AnnotationError("\(phase) needs a normalized headerPos [x, y].")
            }
            for callout in caption.callouts {
                try callout.validate(in: phase)
            }
            previousEnd = caption.end
        }
        return captions
    }
}

private func singleLine(_ value: String) -> Bool {
    !value.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty &&
        value.rangeOfCharacter(from: .newlines) == nil
}

private func normalized(_ values: [CGFloat]) -> Bool {
    values.allSatisfy { (0...1).contains($0) }
}

private func pointIsValid(_ values: [CGFloat]) -> Bool {
    values.count == 2 && normalized(values)
}

private func color(_ hex: UInt32, alpha: CGFloat = 1) -> CGColor {
    NSColor(
        srgbRed: CGFloat((hex >> 16) & 255) / 255,
        green: CGFloat((hex >> 8) & 255) / 255,
        blue: CGFloat(hex & 255) / 255,
        alpha: alpha
    ).cgColor
}

private let accent = color(0xFF3B30)
private let highlightFill = color(0xFF3B30, alpha: 0.10)

private func rounded(_ bounds: CGRect, radius: CGFloat) -> CGPath {
    CGPath(roundedRect: bounds, cornerWidth: radius, cornerHeight: radius, transform: nil)
}

private struct Badge {
    let line: CTLine
    let textWidth: CGFloat
    let ascent: CGFloat
    let descent: CGFloat
    let bounds: CGRect
    let radius: CGFloat

    init(_ text: String, at topLeft: CGPoint?, frame: CGSize, scale: CGFloat, header: Bool = false) {
        func makeLine(_ value: String, size: CGFloat) -> CTLine {
            CTLineCreateWithAttributedString(NSAttributedString(string: value, attributes: [
                NSAttributedString.Key(kCTFontAttributeName as String): NSFont.systemFont(ofSize: size, weight: .bold),
                NSAttributedString.Key(kCTForegroundColorAttributeName as String): color(0xFFFFFF)
            ]))
        }

        let margin = round(14 * scale)
        let padding = round(12 * scale)
        let maxWidth = min(frame.width - 2 * margin, frame.width * (header ? 0.78 : 0.55)) - 2 * padding
        var fontSize = round((header ? 25 : 23) * scale)
        var fitted = makeLine(text, size: fontSize)
        while CTLineGetTypographicBounds(fitted, nil, nil, nil) > maxWidth && fontSize > 11 {
            fontSize -= 1
            fitted = makeLine(text, size: fontSize)
        }
        if CTLineGetTypographicBounds(fitted, nil, nil, nil) > maxWidth {
            fitted = CTLineCreateTruncatedLine(fitted, maxWidth, .end, makeLine("…", size: fontSize)) ?? fitted
        }

        var up: CGFloat = 0
        var down: CGFloat = 0
        let width = CGFloat(CTLineGetTypographicBounds(fitted, &up, &down, nil))
        let badgeWidth = ceil(width) + 2 * padding
        let badgeHeight = ceil(up + down) + 2 * round(6 * scale)
        let x = topLeft?.x ?? frame.width - badgeWidth - margin
        let y = topLeft?.y ?? frame.height - margin
        bounds = CGRect(
            x: round(max(margin, min(x, frame.width - margin - badgeWidth))),
            y: round(max(margin, min(y - badgeHeight, frame.height - margin - badgeHeight))),
            width: badgeWidth, height: badgeHeight
        )
        radius = header ? badgeHeight / 2 : round(7 * scale)
        line = fitted
        textWidth = width
        ascent = up
        descent = down
    }

    func draw(in context: CGContext, scale: CGFloat) {
        context.saveGState()
        context.setShadow(offset: CGSize(width: 0, height: -2 * scale), blur: 4 * scale, color: color(0x220808, alpha: 0.6))
        context.setFillColor(color(0xD92626))
        context.addPath(rounded(bounds, radius: radius))
        context.fillPath()
        context.restoreGState()
        context.textPosition = CGPoint(x: round(bounds.midX - textWidth / 2), y: round(bounds.midY - (ascent - descent) / 2))
        CTLineDraw(line, context)
    }

    func drawArrow(to tip: CGPoint, in context: CGContext, stroke: CGFloat, scale: CGFloat) {
        let x = min(max(tip.x, bounds.minX + radius), bounds.maxX - radius)
        let y = min(max(tip.y, bounds.minY + radius), bounds.maxY - radius)
        let edges = [
            CGPoint(x: bounds.minX, y: y), CGPoint(x: bounds.maxX, y: y),
            CGPoint(x: x, y: bounds.minY), CGPoint(x: x, y: bounds.maxY)
        ]
        func distance(_ edge: CGPoint) -> CGFloat { hypot(tip.x - edge.x, tip.y - edge.y) }
        guard let tail = edges.min(by: { distance($0) < distance($1) }) else { return }
        let length = distance(tail)
        guard length > 1 else { return }

        let ux = (tip.x - tail.x) / length
        let uy = (tip.y - tail.y) / length
        let head = min(15 * scale, length * 0.65)
        let wing = head * 0.45
        let base = CGPoint(x: tip.x - ux * head, y: tip.y - uy * head)
        context.setStrokeColor(accent)
        context.setLineWidth(stroke)
        context.setLineCap(.butt)
        context.move(to: tail)
        context.addLine(to: base)
        context.strokePath()

        context.setFillColor(accent)
        context.move(to: tip)
        context.addLine(to: CGPoint(x: base.x - uy * wing, y: base.y + ux * wing))
        context.addLine(to: CGPoint(x: base.x + uy * wing, y: base.y - ux * wing))
        context.closePath()
        context.fillPath()
    }
}

private func captionImage(_ caption: Caption, size: CGSize) throws -> CGImage {
    guard let context = CGContext(
        data: nil, width: Int(size.width), height: Int(size.height), bitsPerComponent: 8, bytesPerRow: 0,
        space: CGColorSpace(name: CGColorSpace.sRGB) ?? CGColorSpaceCreateDeviceRGB(),
        bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue
    ) else {
        throw AnnotationError("Could not create the annotation bitmap context.")
    }
    context.clear(CGRect(origin: .zero, size: size))
    context.setShouldAntialias(true)
    context.setShouldSmoothFonts(false)
    context.textMatrix = .identity

    let scale = min(1.6, max(0.45, min(size.width / 1440, size.height / 810)))
    let stroke = min(5, max(2, round(4 * scale)))
    func point(_ value: [CGFloat]) -> CGPoint {
        CGPoint(x: value[0] * size.width, y: (1 - value[1]) * size.height)
    }

    context.setStrokeColor(accent)
    context.setFillColor(highlightFill)
    context.setLineWidth(stroke)
    for callout in caption.callouts {
        guard let rect = callout.rect else { continue }
        let bounds = CGRect(
            x: rect[0] * size.width, y: (1 - rect[1] - rect[3]) * size.height,
            width: rect[2] * size.width, height: rect[3] * size.height
        )
        // Keep the stroke inside a target that touches the frame edge.
        let inset = min(stroke / 2, bounds.width / 4, bounds.height / 4)
        context.addPath(rounded(bounds.insetBy(dx: inset, dy: inset), radius: 6 * scale))
        context.drawPath(using: .fillStroke)
    }

    var badges: [Badge] = []
    for callout in caption.callouts {
        guard let label = callout.label, let labelPos = callout.labelPos else { continue }
        let badge = Badge(label, at: point(labelPos), frame: size, scale: scale)
        if let tip = callout.arrowTo {
            badge.drawArrow(to: point(tip), in: context, stroke: stroke, scale: scale)
        }
        badges.append(badge)
    }
    for badge in badges {
        badge.draw(in: context, scale: scale)
    }
    Badge("\(caption.tag) · \(caption.title)", at: caption.headerPos.map(point), frame: size, scale: scale, header: true)
        .draw(in: context, scale: scale)

    guard let image = context.makeImage() else {
        throw AnnotationError("Could not create the annotation image.")
    }
    return image
}

private func annotationLayer(size: CGSize, duration: Double, captions: [Caption]) throws -> CALayer {
    let overlay = CALayer()
    overlay.frame = CGRect(origin: .zero, size: size)

    for caption in captions where caption.start < duration {
        let phase = CALayer()
        phase.frame = overlay.bounds
        phase.opacity = caption.start == 0 ? 1 : 0
        phase.contents = try captionImage(caption, size: size)

        // Discrete opacity changes put each phase on [start, end) in export time.
        var times: [NSNumber] = [0]
        var opacities: [NSNumber] = [NSNumber(value: phase.opacity)]
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
        overlay.addSublayer(phase)
    }
    return overlay
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
    let renderSize = CGSize(width: orientedFrame.width.rounded(), height: orientedFrame.height.rounded())
    guard renderSize.width >= 96, renderSize.height >= 48 else {
        throw AnnotationError("The input frame is too small for on-video annotations.")
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
    let topLeft = orientation.concatenating(
        CGAffineTransform(translationX: -orientedFrame.minX, y: -orientedFrame.minY)
    )
    editorInstruction.setTransform(topLeft, at: .zero)

    let instruction = AVMutableVideoCompositionInstruction()
    instruction.timeRange = CMTimeRange(start: .zero, duration: range.duration)
    instruction.layerInstructions = [editorInstruction]
    instruction.backgroundColor = color(0x000000)

    let canvas = CALayer()
    canvas.frame = CGRect(origin: .zero, size: renderSize)
    let renderedVideo = CALayer()
    renderedVideo.frame = canvas.bounds
    canvas.addSublayer(renderedVideo)
    canvas.addSublayer(try annotationLayer(size: renderSize, duration: duration, captions: captions))

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
