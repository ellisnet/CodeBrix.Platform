// WKWebView's AppKit objects live on this helper process's main thread. The
// PlayTest process owns XAML and exchanges commands/events and PNG snapshots.
#import <AppKit/AppKit.h>
#import <WebKit/WebKit.h>
#include <signal.h>

static void Send(NSDictionary *message)
{
    NSData *data = [NSJSONSerialization dataWithJSONObject:message options:NSJSONWritingFragmentsAllowed error:nil];
    fwrite(data.bytes, 1, data.length, stdout);
    fputc('\n', stdout);
    fflush(stdout);
}

@interface Browser : NSObject <WKNavigationDelegate, WKUIDelegate, WKScriptMessageHandler>
@property WKWebView *view;
@property NSWindow *window;
@property NSMutableDictionary<NSNumber *, id> *policies;
@property NSInteger nextPolicy;
@property NSInteger status;
@property BOOL closed;
- (void)command:(NSDictionary *)command;
@end

@implementation Browser
- (instancetype)init
{
    if ((self = [super init])) {
        _policies = [NSMutableDictionary new];
        WKWebViewConfiguration *configuration = [WKWebViewConfiguration new];
        configuration.websiteDataStore = WKWebsiteDataStore.nonPersistentDataStore;
        configuration.preferences.tabFocusesLinks = YES;
        [configuration.userContentController addScriptMessageHandler:self name:@"codebrixWebView"];
        [configuration.userContentController addUserScript:[[WKUserScript alloc] initWithSource:
            @"window.chrome=window.chrome||{};window.chrome.webview={postMessage:v=>window.webkit.messageHandlers.codebrixWebView.postMessage(v)};"
            injectionTime:WKUserScriptInjectionTimeAtDocumentStart forMainFrameOnly:NO]];
        _view = [[WKWebView alloc] initWithFrame:NSMakeRect(0, 0, 800, 600) configuration:configuration];
        _view.navigationDelegate = self;
        _view.UIDelegate = self;
        _window = [[NSWindow alloc] initWithContentRect:NSMakeRect(-10000, -10000, 800, 600)
            styleMask:NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:NO];
        _window.releasedWhenClosed = NO;
        _window.contentView = _view;
        [_window makeFirstResponder:_view];
        for (NSString *key in @[@"URL", @"title", @"canGoBack", @"canGoForward"])
            [_view addObserver:self forKeyPath:key options:0 context:NULL];
    }
    return self;
}

- (void)observeValueForKeyPath:(NSString *)key ofObject:(id)object change:(NSDictionary *)change context:(void *)context
{
    Send(@{@"event":@"state", @"url":self.view.URL.absoluteString ?: @"about:blank",
        @"title":self.view.title ?: @"", @"back":@(self.view.canGoBack), @"forward":@(self.view.canGoForward)});
}

- (void)webView:(WKWebView *)view decidePolicyForNavigationAction:(WKNavigationAction *)action decisionHandler:(void (^)(WKNavigationActionPolicy))decision
{
    NSNumber *token = @(++self.nextPolicy);
    self.policies[token] = [decision copy];
    Send(@{@"event":@"navigation", @"token":token, @"url":action.request.URL.absoluteString ?: @"about:blank"});
    // A disconnected or unresponsive client must not leave WebKit suspended forever.
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, 15 * NSEC_PER_SEC), dispatch_get_main_queue(), ^{
        void (^pending)(WKNavigationActionPolicy) = self.policies[token];
        if (pending) { [self.policies removeObjectForKey:token]; pending(WKNavigationActionPolicyCancel); }
    });
}

- (void)webView:(WKWebView *)view decidePolicyForNavigationResponse:(WKNavigationResponse *)response decisionHandler:(void (^)(WKNavigationResponsePolicy))decision
{
    if (response.isForMainFrame && [response.response isKindOfClass:NSHTTPURLResponse.class])
        self.status = ((NSHTTPURLResponse *)response.response).statusCode;
    // The offscreen adapter has no download UI or implicit Downloads-folder writes.
    decision(response.canShowMIMEType ? WKNavigationResponsePolicyAllow : WKNavigationResponsePolicyCancel);
}

- (void)completed:(BOOL)success error:(NSError *)error
{
    Send(@{@"event":@"completed", @"url":self.view.URL.absoluteString ?: @"about:blank",
        @"success":@(success), @"status":@(self.status), @"error":error.localizedDescription ?: @""});
}
- (void)webView:(WKWebView *)view didStartProvisionalNavigation:(WKNavigation *)navigation { self.status = 0; }
- (void)webView:(WKWebView *)view didFinishNavigation:(WKNavigation *)navigation { [self completed:YES error:nil]; }
- (void)webView:(WKWebView *)view didFailNavigation:(WKNavigation *)navigation withError:(NSError *)error { [self completed:NO error:error]; }
- (void)webView:(WKWebView *)view didFailProvisionalNavigation:(WKNavigation *)navigation withError:(NSError *)error
{
    if (error.code != NSURLErrorCancelled) [self completed:NO error:error];
}
- (void)webViewWebContentProcessDidTerminate:(WKWebView *)view { Send(@{@"event":@"fatal", @"error":@"The WKWebView content process terminated."}); }
- (void)userContentController:(WKUserContentController *)controller didReceiveScriptMessage:(WKScriptMessage *)message
{
    NSData *json = [NSJSONSerialization dataWithJSONObject:message.body options:NSJSONWritingFragmentsAllowed error:nil];
    Send(@{@"event":@"message", @"json":[[NSString alloc] initWithData:json encoding:NSUTF8StringEncoding] ?: @"null"});
}
- (WKWebView *)webView:(WKWebView *)view createWebViewWithConfiguration:(WKWebViewConfiguration *)configuration forNavigationAction:(WKNavigationAction *)action windowFeatures:(WKWindowFeatures *)features
{
    Send(@{@"event":@"popup", @"url":action.request.URL.absoluteString ?: @"about:blank"});
    return nil;
}
- (void)webView:(WKWebView *)view runJavaScriptAlertPanelWithMessage:(NSString *)message initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(void))completion { completion(); }
- (void)webView:(WKWebView *)view runJavaScriptConfirmPanelWithMessage:(NSString *)message initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(BOOL))completion { completion(NO); }
- (void)webView:(WKWebView *)view runJavaScriptTextInputPanelWithPrompt:(NSString *)prompt defaultText:(NSString *)text initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(NSString *))completion { completion(nil); }
- (void)webView:(WKWebView *)view runOpenPanelWithParameters:(WKOpenPanelParameters *)parameters initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(NSArray<NSURL *> *))completion { completion(nil); }

- (void)command:(NSDictionary *)command
{
    NSNumber *identifier = command[@"id"];
    NSString *op = command[@"op"];
    id result = NSNull.null;
    if ([op isEqual:@"navigate"]) {
        NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:[NSURL URLWithString:command[@"url"]]];
        request.HTTPMethod = command[@"method"] ?: @"GET";
        if ([command[@"headers"] isKindOfClass:NSDictionary.class]) request.allHTTPHeaderFields = command[@"headers"];
        if ([command[@"body"] isKindOfClass:NSString.class]) request.HTTPBody = [[NSData alloc] initWithBase64EncodedString:command[@"body"] options:0];
        [self.view loadRequest:request];
    } else if ([op isEqual:@"html"]) {
        [self.view loadHTMLString:command[@"html"] baseURL:nil];
    } else if ([op isEqual:@"policy"]) {
        void (^decision)(WKNavigationActionPolicy) = self.policies[command[@"token"]];
        if (decision) {
            [self.policies removeObjectForKey:command[@"token"]];
            decision([command[@"allow"] boolValue] ? WKNavigationActionPolicyAllow : WKNavigationActionPolicyCancel);
        }
    } else if ([op isEqual:@"resize"]) {
        double width = [command[@"width"] doubleValue], height = [command[@"height"] doubleValue];
        if (width < 1 || height < 1 || width > 8192 || height > 8192) {
            Send(@{@"id":identifier, @"error":@"Invalid browser dimensions."}); return;
        }
        [self.window setContentSize:NSMakeSize(width, height)];
        self.view.frame = NSMakeRect(0, 0, width, height);
    } else if ([op isEqual:@"snapshot"]) {
        NSSize size = self.view.bounds.size;
        WKSnapshotConfiguration *configuration = [WKSnapshotConfiguration new];
        configuration.snapshotWidth = @(size.width);
        configuration.afterScreenUpdates = NO;
        [self.view takeSnapshotWithConfiguration:configuration completionHandler:^(NSImage *image, NSError *error) {
            if (self.closed) return;
            if (error) { Send(@{@"id":identifier, @"error":error.localizedDescription}); return; }
            CGImageRef cgImage = [image CGImageForProposedRect:NULL context:nil hints:nil];
            NSBitmapImageRep *bitmap = [[NSBitmapImageRep alloc] initWithCGImage:cgImage];
            NSData *png = [bitmap representationUsingType:NSBitmapImageFileTypePNG properties:@{}];
            Send(@{@"id":identifier, @"result":@{@"png":[png base64EncodedStringWithOptions:0], @"width":@(size.width), @"height":@(size.height)}});
        }];
        return;
    } else if ([op isEqual:@"script"]) {
        [self.view evaluateJavaScript:command[@"script"] completionHandler:^(id value, NSError *error) {
            if (self.closed) return;
            if (error) Send(@{@"id":identifier, @"error":error.localizedDescription});
            else {
                NSData *json = [NSJSONSerialization dataWithJSONObject:value ?: NSNull.null options:NSJSONWritingFragmentsAllowed error:nil];
                Send(@{@"id":identifier, @"result":[[NSString alloc] initWithData:json encoding:NSUTF8StringEncoding] ?: @"null"});
            }
        }];
        return;
    } else if ([op isEqual:@"mouse"]) {
        NSPoint location = NSMakePoint([command[@"x"] doubleValue], self.view.bounds.size.height - [command[@"y"] doubleValue]);
        NSInteger kind = [command[@"kind"] integerValue];
        NSInteger button = [command[@"button"] integerValue];
        NSEventType type = kind == 0 ? NSEventTypeMouseMoved : kind == 1
            ? (button == 1 ? NSEventTypeRightMouseDown : button == 2 ? NSEventTypeOtherMouseDown : NSEventTypeLeftMouseDown)
            : (button == 1 ? NSEventTypeRightMouseUp : button == 2 ? NSEventTypeOtherMouseUp : NSEventTypeLeftMouseUp);
        NSEvent *event = [NSEvent mouseEventWithType:type location:location modifierFlags:[command[@"modifiers"] unsignedLongValue]
            timestamp:NSProcessInfo.processInfo.systemUptime windowNumber:self.window.windowNumber context:nil eventNumber:0 clickCount:1 pressure:kind == 1 ? 1 : 0];
        NSView *target = [self.view hitTest:[self.view convertPoint:location fromView:nil]];
        if (kind == 1) [self.window makeFirstResponder:target];
        if (kind == 0) [target mouseMoved:event];
        else if (kind == 1) { if (button == 1) [target rightMouseDown:event]; else if (button == 2) [target otherMouseDown:event]; else [target mouseDown:event]; }
        else { if (button == 1) [target rightMouseUp:event]; else if (button == 2) [target otherMouseUp:event]; else [target mouseUp:event]; }
    } else if ([op isEqual:@"key"]) {
        NSEvent *event = [NSEvent keyEventWithType:[command[@"down"] boolValue] ? NSEventTypeKeyDown : NSEventTypeKeyUp
            location:NSZeroPoint modifierFlags:[command[@"modifiers"] unsignedLongValue] timestamp:NSProcessInfo.processInfo.systemUptime
            windowNumber:self.window.windowNumber context:nil characters:command[@"text"] charactersIgnoringModifiers:command[@"text"]
            isARepeat:NO keyCode:[command[@"code"] unsignedShortValue]];
        if ([command[@"down"] boolValue]) [self.window.firstResponder keyDown:event];
        else [self.window.firstResponder keyUp:event];
    } else if ([op isEqual:@"scroll"]) {
        CGEventRef cg = CGEventCreateScrollWheelEvent(NULL, kCGScrollEventUnitPixel, 2,
            [command[@"dy"] intValue], [command[@"dx"] intValue]);
        NSEvent *event = [NSEvent eventWithCGEvent:cg];
        [self.view scrollWheel:event];
        CFRelease(cg);
    } else if ([op isEqual:@"back"]) [self.view goBack];
    else if ([op isEqual:@"forward"]) [self.view goForward];
    else if ([op isEqual:@"reload"]) [self.view reload];
    else if ([op isEqual:@"stop"]) [self.view stopLoading];
    else if ([op isEqual:@"userAgent"]) self.view.customUserAgent = [command[@"value"] length] ? command[@"value"] : nil;
    else { Send(@{@"id":identifier, @"error":@"Unknown browser command."}); return; }
    Send(@{@"id":identifier, @"result":result});
}
@end

int main(int argc, const char *argv[])
{
    @autoreleasepool {
        signal(SIGPIPE, SIG_IGN);
        [NSApplication sharedApplication];
        [NSApp setActivationPolicy:NSApplicationActivationPolicyProhibited];
        Browser *browser = [Browser new];
        dispatch_async(dispatch_get_global_queue(QOS_CLASS_USER_INITIATED, 0), ^{
            char *line = NULL;
            size_t capacity = 0;
            ssize_t length;
            while ((length = getline(&line, &capacity, stdin)) != -1) {
                @autoreleasepool {
                    NSData *data = [NSData dataWithBytes:line length:(NSUInteger)length];
                    NSDictionary *command = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
                    if (![command isKindOfClass:NSDictionary.class] || ![command[@"id"] isKindOfClass:NSNumber.class]) exit(2);
                    dispatch_async(dispatch_get_main_queue(), ^{
                        @try { [browser command:command]; }
                        @catch (NSException *exception) { Send(@{@"id":command[@"id"], @"error":exception.reason ?: exception.name}); }
                    });
                }
            }
            free(line);
            dispatch_async(dispatch_get_main_queue(), ^{
                browser.closed = YES;
                [browser.view stopLoading];
                [NSApp terminate:nil];
            });
        });
        Send(@{@"event":@"ready"});
        [NSApp run];
    }
    return 0;
}
