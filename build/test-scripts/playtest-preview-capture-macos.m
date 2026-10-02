// Capture only the SDL preview belonging to the supplied PID. macOS requires
// Screen Recording permission for the terminal/agent running this check.
#import <AppKit/AppKit.h>
#import <ScreenCaptureKit/ScreenCaptureKit.h>

static void Fail(NSString *message) { fprintf(stderr, "%s\n", message.UTF8String); exit(1); }

int main(int argc, const char *argv[])
{
    @autoreleasepool {
        if (argc != 5) return 2;
        [NSApplication sharedApplication];
        [NSApp setActivationPolicy:NSApplicationActivationPolicyProhibited];
        pid_t pid = atoi(argv[1]);
        NSInteger clientWidth = atoi(argv[2]), clientHeight = atoi(argv[3]);
        NSString *path = [NSString stringWithUTF8String:argv[4]];
        [SCShareableContent getShareableContentExcludingDesktopWindows:YES onScreenWindowsOnly:YES completionHandler:^(SCShareableContent *content, NSError *error) {
            if (error) Fail(error.localizedDescription);
            SCWindow *window = nil;
            for (SCWindow *candidate in content.windows)
                if (candidate.owningApplication.processID == pid && [candidate.title hasPrefix:@"CodeBrix PlayTest"]) { window = candidate; break; }
            if (!window) Fail(@"The requested PlayTest preview is not visible.");
            SCContentFilter *filter = [[SCContentFilter alloc] initWithDesktopIndependentWindow:window];
            SCStreamConfiguration *config = [SCStreamConfiguration new];
            config.width = (size_t)window.frame.size.width;
            config.height = (size_t)window.frame.size.height;
            config.showsCursor = NO;
            config.pixelFormat = kCVPixelFormatType_32BGRA;
            config.colorSpaceName = kCGColorSpaceSRGB;
            config.ignoreShadowsSingleWindow = YES;
            [SCScreenshotManager captureImageWithFilter:filter configuration:config completionHandler:^(CGImageRef image, NSError *captureError) {
                if (captureError) Fail(captureError.localizedDescription);
                size_t width = CGImageGetWidth(image), height = CGImageGetHeight(image);
                if (width != clientWidth || height < clientHeight) Fail(@"Unexpected preview window dimensions.");
                CGImageRef crop = CGImageCreateWithImageInRect(image, CGRectMake(0, height - clientHeight, clientWidth, clientHeight));
                CGColorSpaceRef space = CGColorSpaceCreateWithName(kCGColorSpaceSRGB);
                CGContextRef context = CGBitmapContextCreate(NULL, clientWidth, clientHeight, 8, clientWidth*4,
                    space, kCGImageAlphaPremultipliedLast | kCGBitmapByteOrder32Big);
                CGContextDrawImage(context, CGRectMake(0,0,clientWidth,clientHeight), crop);
                CGImageRef normalized = CGBitmapContextCreateImage(context);
                NSBitmapImageRep *bitmap = [[NSBitmapImageRep alloc] initWithCGImage:normalized];
                [[bitmap representationUsingType:NSBitmapImageFileTypePNG properties:@{}] writeToFile:path atomically:YES];
                unsigned char *pixels = CGBitmapContextGetData(context);
                unsigned char *center = pixels + (clientHeight/2*clientWidth+clientWidth/2)*4;
                NSInteger left = clientWidth, top = clientHeight, right = 0, bottom = 0;
                for (NSInteger y = 0; y < clientHeight; y++) for (NSInteger x = 0; x < clientWidth; x++) {
                    unsigned char *pixel = pixels + (y*clientWidth+x)*4;
                    if (pixel[0] > 8 || pixel[1] > 8 || pixel[2] > 8) {
                        left = MIN(left, x); top = MIN(top, y); right = MAX(right, x+1); bottom = MAX(bottom, y+1);
                    }
                }
                NSDictionary *result = @{@"size":@[@(width),@(height)], @"client":@[@(clientWidth),@(clientHeight)],
                    @"center":@[@(center[0]),@(center[1]),@(center[2])], @"bounds":@[@(left),@(top),@(right),@(bottom)]};
                NSData *json = [NSJSONSerialization dataWithJSONObject:result options:0 error:nil];
                fwrite(json.bytes, 1, json.length, stdout); fputc('\n',stdout);
                CGImageRelease(crop);
                CGImageRelease(normalized);
                CGContextRelease(context);
                CGColorSpaceRelease(space);
                exit(0);
            }];
        }];
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, 15*NSEC_PER_SEC), dispatch_get_main_queue(), ^{ Fail(@"Screen capture timed out."); });
        dispatch_main();
    }
}
