//
//  CDBRXWebView.m
//

#import "CDBRXNative.h"
#import "CDBRXWebView.h"

static codebrix_webview_javascript_fn_ptr execute;
static codebrix_webview_javascript_fn_ptr invoke;

inline codebrix_webview_javascript_fn_ptr codebrix_get_execute_callback(void)
{
    return execute;
}

void codebrix_set_execute_callback(codebrix_webview_javascript_fn_ptr p)
{
    execute = p;
}

inline codebrix_webview_javascript_fn_ptr codebrix_get_invoke_callback(void)
{
    return invoke;
}

void codebrix_set_invoke_callback(codebrix_webview_javascript_fn_ptr p)
{
    invoke = p;
}

static codebrix_webview_navigation_starting_fn_ptr navigation_starting;
static codebrix_webview_navigation_finishing_fn_ptr navigation_finishing;
static codebrix_webview_webview_notification_fn_ptr property_change_notification;
static codebrix_webview_navigation_finishing_fn_ptr receive_web_message;
static codebrix_webview_navigation_failing_fn_ptr navigation_failing;

inline codebrix_webview_navigation_starting_fn_ptr codebrix_get_webview_navigation_starting_callback(void)
{
    return navigation_starting;
}

inline codebrix_webview_navigation_finishing_fn_ptr codebrix_get_webview_navigation_finishing_callback(void)
{
    return navigation_finishing;
}

inline codebrix_webview_webview_notification_fn_ptr codebrix_get_webview_notification_callback(void)
{
    return property_change_notification;
}

inline codebrix_webview_navigation_finishing_fn_ptr codebrix_get_webview_receive_web_message_callback(void)
{
    return receive_web_message;
}

inline codebrix_webview_navigation_failing_fn_ptr codebrix_get_webview_navigation_failing_callback(void)
{
    return navigation_failing;
}

void codebrix_set_webview_navigation_callbacks(codebrix_webview_navigation_starting_fn_ptr starting, codebrix_webview_navigation_finishing_fn_ptr finishing, codebrix_webview_webview_notification_fn_ptr notification, codebrix_webview_navigation_finishing_fn_ptr web_message, codebrix_webview_navigation_failing_fn_ptr failing)
{
    navigation_starting = starting;
    navigation_finishing = finishing;
    property_change_notification = notification;
    receive_web_message = web_message;
    navigation_failing = failing;
}

static codebrix_webview_unsupported_scheme_identified_fn_ptr unsupported_scheme_identified;

inline codebrix_webview_unsupported_scheme_identified_fn_ptr codebrix_get_webview_unsupported_scheme_identified_callback(void)
{
    return unsupported_scheme_identified;
}

void codebrix_set_webview_unsupported_scheme_identified_callback(codebrix_webview_unsupported_scheme_identified_fn_ptr fn_ptr)
{
    unsupported_scheme_identified = fn_ptr;
}

// downloads (WKDownload, macOS 11.3+)

static codebrix_webview_download_starting_fn_ptr download_starting;
static codebrix_webview_download_progress_fn_ptr download_progress;
static codebrix_webview_download_finished_fn_ptr download_finished;

void codebrix_set_webview_download_callbacks(codebrix_webview_download_starting_fn_ptr starting, codebrix_webview_download_progress_fn_ptr progress, codebrix_webview_download_finished_fn_ptr finished)
{
    download_starting = starting;
    download_progress = progress;
    download_finished = finished;
}

// One delegate per WKDownload. WKDownload.delegate is weak, so the delegates (which in turn
// retain their download) are kept alive in this table until the download finishes or fails.
static NSMutableDictionary<NSValue*, id> *download_delegates;

API_AVAILABLE(macos(11.3))
@interface CDBRXDownloadDelegate : NSObject <WKDownloadDelegate>
@property (nonatomic, weak, nullable) WKWebView *webview;
@property (nonatomic, retain, nullable) WKDownload *download;
@property (nonatomic, copy, nullable) void (^destinationHandler)(NSURL * _Nullable);
@property (nonatomic, retain, nullable) NSProgress *observedProgress;
@end

API_AVAILABLE(macos(11.3))
static CDBRXDownloadDelegate* codebrix_download_delegate_get(void *download)
{
    return download == NULL ? nil : [download_delegates objectForKey:[NSValue valueWithPointer:download]];
}

API_AVAILABLE(macos(11.3))
static void codebrix_download_attach(WKWebView *webview, WKDownload *download)
{
    if (download_delegates == nil) {
        download_delegates = [[NSMutableDictionary alloc] init];
    }
    CDBRXDownloadDelegate *delegate = [[CDBRXDownloadDelegate alloc] init];
    delegate.webview = webview;
    delegate.download = download;
    [download_delegates setObject:delegate forKey:[NSValue valueWithPointer:(__bridge void*)download]];
    download.delegate = delegate;
#if DEBUG
    NSLog(@"codebrix_download_attach webView %p download %p", webview, download);
#endif
}

@implementation CDBRXDownloadDelegate

- (void)download:(WKDownload *)download decideDestinationUsingResponse:(NSURLResponse *)response suggestedFilename:(NSString *)suggestedFilename completionHandler:(void (^)(NSURL * _Nullable))completionHandler API_AVAILABLE(macos(11.3))
{
    self.destinationHandler = completionHandler;

    const char *url = response.URL.absoluteString.UTF8String;
    const char *mimeType = response.MIMEType.UTF8String;
    const char *contentDisposition = NULL;
    if ([response isKindOfClass:NSHTTPURLResponse.class]) {
        contentDisposition = [((NSHTTPURLResponse*)response) valueForHTTPHeaderField:@"Content-Disposition"].UTF8String;
    }
    int64_t totalBytes = response.expectedContentLength > 0 ? response.expectedContentLength : 0;
#if DEBUG
    NSLog(@"decideDestinationUsingResponse download %p URL: %@ suggested %@", download, response.URL, suggestedFilename);
#endif
    if (download_starting != NULL) {
        // The completion handler stays parked until the managed side answers through
        // codebrix_webview_download_set_destination or codebrix_webview_download_cancel.
        download_starting(self.webview, (__bridge void*)download, url, mimeType, contentDisposition, totalBytes, suggestedFilename.UTF8String);
    } else {
        completionHandler(nil); // no managed handler registered: refuse the download
        self.destinationHandler = nil;
    }
}

- (void)observeValueForKeyPath:(NSString *)keyPath ofObject:(id)object change:(NSDictionary *)change context:(void *)context
{
    if (![keyPath isEqualToString:@"completedUnitCount"]) {
        return;
    }
    NSProgress *progress = (NSProgress*)object;
    int64_t received = progress.completedUnitCount;
    int64_t total = progress.totalUnitCount > 0 ? progress.totalUnitCount : 0;
    void *download;
    if (@available(macOS 11.3, *)) {
        download = (__bridge void*)self.download;
    } else {
        return;
    }
    // NSProgress KVO can fire on a background queue; the managed callback expects the UI thread.
    dispatch_async(dispatch_get_main_queue(), ^{
        if (download_progress != NULL) {
            download_progress(download, received, total);
        }
    });
}

- (void)startObservingProgress API_AVAILABLE(macos(11.3))
{
    if (self.observedProgress == nil && self.download != nil) {
        self.observedProgress = self.download.progress;
        [self.observedProgress addObserver:self forKeyPath:@"completedUnitCount" options:NSKeyValueObservingOptionNew context:NULL];
    }
}

- (void)finishWithSuccess:(int32_t)success wasCancelled:(int32_t)wasCancelled errorMessage:(const char*)errorMessage API_AVAILABLE(macos(11.3))
{
    if (self.observedProgress != nil) {
        [self.observedProgress removeObserver:self forKeyPath:@"completedUnitCount"];
        self.observedProgress = nil;
    }
    void *download = (__bridge void*)self.download;
    if (download_finished != NULL) {
        download_finished(download, success, wasCancelled, errorMessage);
    }
    self.destinationHandler = nil;
    [download_delegates removeObjectForKey:[NSValue valueWithPointer:download]];
}

- (void)downloadDidFinish:(WKDownload *)download API_AVAILABLE(macos(11.3))
{
#if DEBUG
    NSLog(@"downloadDidFinish download %p", download);
#endif
    [self finishWithSuccess:1 wasCancelled:0 errorMessage:NULL];
}

- (void)download:(WKDownload *)download didFailWithError:(NSError *)error resumeData:(NSData *)resumeData API_AVAILABLE(macos(11.3))
{
#if DEBUG
    NSLog(@"download %p didFailWithError %@", download, error);
#endif
    int32_t wasCancelled = [error.domain isEqualToString:NSURLErrorDomain] && error.code == NSURLErrorCancelled;
    [self finishWithSuccess:0 wasCancelled:wasCancelled errorMessage:error.localizedDescription.UTF8String];
}

@end

void codebrix_webview_download_set_destination(void *download, const char *path)
{
    if (@available(macOS 11.3, *)) {
        CDBRXDownloadDelegate *delegate = codebrix_download_delegate_get(download);
#if DEBUG
        NSLog(@"codebrix_webview_download_set_destination download %p path %s", download, path);
#endif
        if (delegate != nil && delegate.destinationHandler != nil) {
            NSURL *destination = [NSURL fileURLWithPath:[NSString stringWithUTF8String:path]];
            void (^handler)(NSURL * _Nullable) = delegate.destinationHandler;
            delegate.destinationHandler = nil;
            handler(destination);
            [delegate startObservingProgress];
        }
    }
}

void codebrix_webview_download_cancel(void *download)
{
    if (@available(macOS 11.3, *)) {
        CDBRXDownloadDelegate *delegate = codebrix_download_delegate_get(download);
#if DEBUG
        NSLog(@"codebrix_webview_download_cancel download %p", download);
#endif
        if (delegate == nil) {
            return;
        }
        if (delegate.destinationHandler != nil) {
            // Still parked at the destination decision: a nil destination cancels the download,
            // which then reports didFailWithError (NSURLErrorCancelled).
            void (^handler)(NSURL * _Nullable) = delegate.destinationHandler;
            delegate.destinationHandler = nil;
            handler(nil);
        } else {
            [delegate.download cancel:^(NSData * _Nullable resumeData) { }];
        }
    }
}


NSView* codebrix_webview_create(NSWindow *window, const char *ok, const char *cancel)
{
    WKWebViewConfiguration* config = [[WKWebViewConfiguration alloc] init];
    if (@available(macOS 11, *)) {
        config.defaultWebpagePreferences.allowsContentJavaScript = YES;
    } else {
        // dotnet 9 still supports macOS 10.15
        config.preferences.javaScriptEnabled = YES;
    }
    config.preferences.javaScriptCanOpenWindowsAutomatically = YES;
    config.mediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypeVideo | WKAudiovisualMediaTypeAudio;
    
    UNOWebView* webview = [[UNOWebView alloc] initWithFrame:NSMakeRect(0,0,0,0) configuration:config];
#if DEBUG
    NSLog(@"codebrix_webview_create %p", webview);
#endif
    webview.okString = [NSString stringWithUTF8String:ok];
    webview.cancelString = [NSString stringWithUTF8String:cancel];

    webview.originalSuperView = window.contentViewController.view;
    return webview;
}

const char* codebrix_webview_get_title(WKWebView *webview)
{
    return strdup(webview.title.UTF8String);
}

bool codebrix_webview_can_go_back(WKWebView *webview)
{
    return webview.canGoBack;
}

bool codebrix_webview_can_go_forward(WKWebView *webview)
{
    return webview.canGoForward;
}

void codebrix_webview_go_back(WKWebView *webview)
{
#if DEBUG
    NSLog(@"codebrix_webview_go_back %p", webview);
#endif
    [webview goBack];
}

void codebrix_webview_go_forward(WKWebView *webview)
{
#if DEBUG
    NSLog(@"codebrix_webview_go_forward %p", webview);
#endif
    [webview goForward];
}

void codebrix_webview_navigate(WKWebView *webview, const char* url, const char *jsonHeaders)
{
    NSString *s = [NSString stringWithUTF8String:url];
    // note that if the app is not bundled then we get `/usr/local/share/dotnet/` and won't find the resources
    // this is covered inside managed code
    if (codebrix_application_is_bundled()) {
        // convert [BundlePath] and [ResourcePath] to bundle specific paths
        s = [s stringByReplacingOccurrencesOfString:@"[BundlePath]" withString:NSBundle.mainBundle.bundlePath];
        s = [s stringByReplacingOccurrencesOfString:@"[ResourcePath]" withString:NSBundle.mainBundle.resourcePath];
    }
    
    NSArray *headers = nil;
    if (jsonHeaders) {
        NSString *h = [NSString stringWithUTF8String:jsonHeaders];
        NSData *d = [h dataUsingEncoding:NSUTF8StringEncoding];
        NSError *e = nil;
        headers = [NSJSONSerialization JSONObjectWithData:d options:NSJSONReadingMutableContainers error: &e];
#if DEBUG
        if (e) {
            NSLog(@"codebrix_webview_navigate %p headers %@ error: %@", webview, headers, e);
        }
#endif
    }
    
    NSURL *u = [NSURL URLWithString:s];
    NSMutableURLRequest *r = [NSMutableURLRequest requestWithURL:u];
    if (headers) {
        // decode the JSON object into key and value strings
        for (int i = 0; i < [headers count]; i++) {
            NSDictionary* keyvalue = [headers objectAtIndex:i];
            NSString *key = keyvalue[@"Key"];
            NSArray *value = keyvalue[@"Value"];
            [r setValue:value.firstObject forHTTPHeaderField:key];
        }
    }
#if DEBUG
    NSLog(@"codebrix_webview_navigate %p url: %@ headers %@", webview, s, headers);
#endif
    [webview loadRequest:r];
}

void codebrix_webview_load_html(WKWebView *webview, const char* html)
{
    NSString *data = [NSString stringWithUTF8String:html];
#if DEBUG
    NSLog(@"codebrix_webview_load_html %@", data);
#endif
    // a nil baseURL slows downs things causing timeouts in tests
    NSURL* blank = [NSURL URLWithString:@"about:blank"];
    [webview loadHTMLString:data baseURL:blank];
}

void codebrix_webview_reload(WKWebView *webview)
{
#if DEBUG
    NSLog(@"codebrix_webview_reload %p", webview);
#endif
    [webview reload];
}

void codebrix_webview_stop(WKWebView *webview)
{
#if DEBUG
    NSLog(@"codebrix_webview_stop %p", webview);
#endif
    [webview stopLoading];
}

void codebrix_webview_execute_script(WKWebView *webview, NSInteger handle, const char *javascript)
{
#if DEBUG
    NSLog(@"codebrix_webview_execute_script %p handle: 0x%lx javascript: %s", webview, handle, javascript);
#endif
    NSString *js = [NSString stringWithUTF8String:javascript];
    [webview evaluateJavaScript:js completionHandler:^(NSObject* result, NSError *error) {
        const char* r = nil;
        const char* e = nil;
        if (error) {
#if DEBUG
            NSLog(@"codebrix_webview_execute_script %p completionHandler error: %@", webview, error);
#endif
            e = error.description.UTF8String;
        } else if (result != nil) {
            if ([NSJSONSerialization isValidJSONObject:result]) {
#if DEBUG
                NSLog(@"codebrix_webview_execute_script %p completionHandler json: %@ string %@", webview, result, ((NSObject*)result).description);
#endif
                NSError *jsonError;
                NSData *data = [NSJSONSerialization dataWithJSONObject:result options:0 error:&jsonError];
                if (jsonError) {
#if DEBUG
                    NSLog(@"codebrix_webview_execute_script %p completionHandler jsonError: %@", webview, jsonError);
#endif
                    e = jsonError.description.UTF8String;
                } else {
#if DEBUG
                    NSLog(@"codebrix_webview_execute_script %p completionHandler data: %@", webview, data);
#endif
                    r = [[[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding] UTF8String];
                }
            } else if ([result isKindOfClass:NSString.class]) {
                // double quote existing quotes so we can quote the whole thing
                NSString *s = [((NSString*) result) stringByReplacingOccurrencesOfString:@"\"" withString:@"\\\""];
                NSString *quoted = [NSString stringWithFormat:@"\"%@\"", s];
#if DEBUG
                NSLog(@"codebrix_webview_execute_script %p completionHandler string: %@", webview, quoted);
#endif
                r = quoted.UTF8String;
            } else {
                NSString *s = result.description;
#if DEBUG
                NSLog(@"codebrix_webview_execute_script %p completionHandler object: %@", webview, s);
#endif
                r = s.UTF8String;
            }
        }
        codebrix_get_execute_callback()(handle, r, e);
    }];
}

void codebrix_webview_invoke_script(WKWebView *webview, NSInteger handle, const char *javascript)
{
#if DEBUG
    NSLog(@"codebrix_webview_invoke_script %p handle: 0x%lx javascript: %s", webview, handle, javascript);
#endif
    NSString *js = [NSString stringWithUTF8String:javascript];
    [webview evaluateJavaScript:js completionHandler:^(NSString *result, NSError *error) {
#if DEBUG
        NSLog(@"codebrix_webview_invoke_script %p completionHandler result: %@ error: %@", webview, result, error);
#endif
        const char* r = result ? result.UTF8String : nil;
        const char* e = error ? error.description.UTF8String : nil;
        codebrix_get_invoke_callback()(handle, r, e);
    }];
}

void codebrix_webview_set_scrolling_enabled(UNOWebView* webview, bool enabled)
{
    webview.scrollingEnabled = enabled;
}

void codebrix_webview_set_user_agent(WKWebView* webview, const char* user_agent)
{
    // nil restores WKWebView's default User-Agent.
    webview.customUserAgent = user_agent ? [NSString stringWithUTF8String:user_agent] : nil;
}

@implementation UNOWebView : WKWebView

- (nullable instancetype)initWithCoder:(NSCoder *)coder {
    return [super initWithCoder:coder];
}

- (instancetype)initWithFrame:(CGRect)frame configuration:(WKWebViewConfiguration *)configuration {
    self = [super initWithFrame:frame configuration:configuration];
    if (self) {
        self.UIDelegate = self;
        self.navigationDelegate = self;
        self.scrollingEnabled = true;
        
        __weak id weakSelf = self;
        [configuration.userContentController addScriptMessageHandler:weakSelf name:@"unoWebView"];
    }
    return self;
}

- (void)didChangeValueForKey:(NSString *)key {
    NSInteger keyId = -1;
    if ([key isEqualToString:@"title"]) {
#if DEBUG
        NSLog(@"didChangeValueForKey webview %p key: %@ value: %@", self, key, self.title);
#endif
        keyId = 0;
    } else if ([key isEqualToString:@"URL"]) {
#if DEBUG
        NSLog(@"didChangeValueForKey webview %p key: %@ value: %@", self, key, self.URL);
#endif
        keyId = 1;
    } else if ([key isEqualToString:@"canGoBack"]) {
#if DEBUG
        NSLog(@"didChangeValueForKey webview %p key: %@ value: %s", self, key, self.canGoBack ? "TRUE" : "FALSE");
#endif
        keyId = 2;
    } else if ([key isEqualToString:@"canGoForward"]) {
#if DEBUG
        NSLog(@"didChangeValueForKey webview %p key: %@ value: %s", self, key, self.canGoForward ? "TRUE" : "FALSE");
#endif
        keyId = 3;
    }
    if (keyId != -1) {
        codebrix_get_webview_notification_callback()(self, keyId);
    }
    [super didChangeValueForKey:key];
}

- (void)scrollWheel:(NSEvent *)event {
    if (self.scrollingEnabled) {
        [super scrollWheel:event];
    } else {
        [[self nextResponder] scrollWheel:event];
    }
}

// UNONativeElement

- (void) dispose {
#if DEBUG
    NSLog(@"UNOWebView %p disposing with superview %p", self, self.superview);
#endif
    if (self.superview) {
        [self stopLoading];
        [self removeFromSuperview];
    }
    [self.configuration.userContentController removeScriptMessageHandlerForName:@"unoWebView"];
}

// WKUIDelegate

- (void)webView:(WKWebView *)webView runJavaScriptConfirmPanelWithMessage:(NSString *)message initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(BOOL result))completionHandler {
#if DEBUG
    NSLog(@"runJavaScriptConfirmPanel webview %p message: %@", webView, message);
#endif
    NSAlert *alert = [[NSAlert alloc] init];
    alert.alertStyle = NSAlertStyleInformational;
    alert.informativeText = message;
    
    UNOWebView *uno = (UNOWebView*) webView;
    [alert addButtonWithTitle:uno.okString];
    [alert addButtonWithTitle:uno.cancelString];
    [alert beginSheetModalForWindow:webView.window completionHandler:^(NSModalResponse returnCode) {
        completionHandler(returnCode == NSAlertFirstButtonReturn);
    }];
}

- (void)webView:(WKWebView *)webView runJavaScriptAlertPanelWithMessage:(NSString *)message initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(void))completionHandler {
#if DEBUG
    NSLog(@"runJavaScriptAlertPanel webview %p message: %@", webView, message);
#endif
    NSAlert *alert = [[NSAlert alloc] init];
    alert.alertStyle = NSAlertStyleInformational;
    alert.informativeText = message;
    [alert runModal];
    completionHandler();
}

- (void)webView:(WKWebView *)webView runJavaScriptTextInputPanelWithPrompt:(NSString *)prompt defaultText:(nullable NSString *)defaultText initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(NSString * _Nullable result))completionHandler {
#if DEBUG
    NSLog(@"runJavaScriptTextInputPanelWithPrompt webview %p prompt: %@", webView, prompt);
#endif
    NSTextField *tf = [[NSTextField alloc] initWithFrame:NSMakeRect(0, 0, 300, 20)];
    tf.placeholderString = defaultText;
    
    NSAlert *alert = [[NSAlert alloc] init];
    alert.alertStyle = NSAlertStyleInformational;
    alert.informativeText = prompt;
    alert.accessoryView = tf;
    
    UNOWebView *uno = (UNOWebView*) webView;
    [alert addButtonWithTitle:uno.okString];
    [alert addButtonWithTitle:uno.cancelString];
    [alert beginSheetModalForWindow:webView.window completionHandler:^(NSModalResponse returnCode) {
        completionHandler(returnCode == NSAlertFirstButtonReturn ? tf.stringValue : nil);
    }];
}

- (void)webView:(WKWebView *)webView runOpenPanelWithParameters:(WKOpenPanelParameters *)parameters initiatedByFrame:(WKFrameInfo *)frame completionHandler:(void (^)(NSArray<NSURL *> * _Nullable URLs))completionHandler {

    NSOpenPanel *openPanel = [NSOpenPanel openPanel];
    openPanel.allowsMultipleSelection = parameters.allowsMultipleSelection;
    if (@available(macOS 10.13.4, *)) {
        openPanel.canChooseDirectories = parameters.allowsDirectories;
    }
    [openPanel beginSheetModalForWindow:webView.window completionHandler:^(NSInteger result) {
        if (result == NSModalResponseOK)
            completionHandler(openPanel.URLs);
        else
            completionHandler(nil);
    }];
}

static codebrix_webview_new_window_requested_fn_ptr new_window_requested;

inline codebrix_webview_new_window_requested_fn_ptr codebrix_get_webview_new_window_requested_callback(void)
{
    return new_window_requested;
}

void codebrix_set_webview_new_window_requested_callback(codebrix_webview_new_window_requested_fn_ptr fn_ptr)
{
    new_window_requested = fn_ptr;
}

- (WKWebView *)webView:(WKWebView *)webView
createWebViewWithConfiguration:(WKWebViewConfiguration *)configuration
           forNavigationAction:(WKNavigationAction *)navigationAction
                windowFeatures:(WKWindowFeatures *)windowFeatures {

#if DEBUG
    NSLog(@"createWebViewWithConfiguration: fired for URL: %@", navigationAction.request.URL);
#endif

    codebrix_webview_new_window_requested_fn_ptr callback = codebrix_get_webview_new_window_requested_callback();
    
    assert(callback);
#if DEBUG
    NSLog(@"createWebViewWithConfiguration: Found C# callback. Attempting to call...");
#endif
        
    const char* targetUrl = [navigationAction.request.URL.absoluteString UTF8String];
    if (targetUrl == nil) {
        targetUrl = "about:blank";
    }
    
    const char* refererUrl = [navigationAction.sourceFrame.request.URL.absoluteString UTF8String];
    if (refererUrl == nil) {
        refererUrl = "about:blank";
    }

    int handled = callback(webView, targetUrl, refererUrl);

#if DEBUG
    NSLog(@"createWebViewWithConfiguration: C# callback returned: %d", handled);
#endif

    // Check if C# handled it (returned 1)
    if (handled == 1) {
#if DEBUG
        NSLog(@"createWebViewWithConfiguration: C# handled the request. Cancelling native new window.");
#endif
        return nil;
    }

#if DEBUG
    NSLog(@"createWebViewWithConfiguration: C# did not handle. Opening in default browser.");
#endif
    if (navigationAction.request.URL) {
        [[NSWorkspace sharedWorkspace] openURL:navigationAction.request.URL];
    }
    
    return nil;
}

// WKNavigationDelegate

// note: this is NOT called when navigating to a different anchor of the same URL
- (void)webView:(WKWebView *)webView didStartProvisionalNavigation:(null_unspecified WKNavigation *)navigation {
#if DEBUG
    NSLog(@"didStartProvisionalNavigation webView %p nagivation %@ URL: %@", webView, navigation, webView.URL);
#endif
    codebrix_get_webview_navigation_starting_callback()(webView, webView.URL.absoluteString.UTF8String);
}

- (void)webView:(WKWebView *)webView didFailProvisionalNavigation:(null_unspecified WKNavigation *)navigation withError:(NSError *)error {
#if DEBUG
    NSLog(@"didFailProvisionalNavigation webView %p nagivation %@ URL: %@ error: %@", webView, navigation, webView.URL, error);
#endif
    int status = -1;
    // based on uno/src/Platform.UI/UI/Xaml/Controls/WebView/Native/iOSmacOS/CodeBrixWKWebView.ErrorMap.iOSmacOS.cs
    switch(error.code) {
        case NSURLErrorCannotLoadFromNetwork:
            status = CoreWebView2WebErrorStatusServerUnreachable;
            break;
        case NSURLErrorClientCertificateRequired:
            status = CoreWebView2WebErrorStatusValidAuthenticationCredentialsRequired;
            break;
        case NSURLErrorClientCertificateRejected:
            status = CoreWebView2WebErrorStatusClientCertificateContainsErrors;
            break;
        case NSURLErrorServerCertificateNotYetValid:
        case NSURLErrorServerCertificateHasUnknownRoot:
            status = CoreWebView2WebErrorStatusCertificateIsInvalid;
            break;
        case NSURLErrorServerCertificateHasBadDate:
            status = CoreWebView2WebErrorStatusCertificateExpired;
            break;
        case NSURLErrorSecureConnectionFailed:
            status = CoreWebView2WebErrorStatusServerUnreachable;
            break;
        case NSURLErrorAppTransportSecurityRequiresSecureConnection:
            status = CoreWebView2WebErrorStatusCannotConnect;
            break;
        case NSURLErrorUserAuthenticationRequired:
            status = CoreWebView2WebErrorStatusValidAuthenticationCredentialsRequired;
            break;
        case NSURLErrorBadServerResponse:
            status = CoreWebView2WebErrorStatusErrorHttpInvalidServerResponse;
            break;
        case NSURLErrorServerCertificateUntrusted:
            status = CoreWebView2WebErrorStatusCertificateIsInvalid;
            break;
        case NSURLErrorRedirectToNonExistentLocation:
        case NSURLErrorHTTPTooManyRedirects:
            status = CoreWebView2WebErrorStatusRedirectFailed;
            break;
        case NSURLErrorResourceUnavailable:
            status = CoreWebView2WebErrorStatusServerUnreachable;
            break;
        case NSURLErrorDNSLookupFailed:
        case NSURLErrorCannotFindHost:
            status = CoreWebView2WebErrorStatusHostNameNotResolved;
            break;
        case NSURLErrorNetworkConnectionLost:
        case NSURLErrorCannotConnectToHost:
            status = CoreWebView2WebErrorStatusServerUnreachable;
            break;
        case NSURLErrorTimedOut:
            status = CoreWebView2WebErrorStatusTimeout;
            break;
        case NSURLErrorCancelled:
            status = CoreWebView2WebErrorStatusOperationCanceled;
            break;
        default:
            status = CoreWebView2WebErrorStatusUnexpectedError;
            break;
    }
    codebrix_get_webview_navigation_failing_callback()(webView, webView.URL.absoluteString.UTF8String, status);
}

// note: this is NOT called when navigating to a different anchor of the same URL
- (void)webView:(WKWebView *)webView didFinishNavigation:(null_unspecified WKNavigation *)navigation {
#if DEBUG
    NSLog(@"didFinishNavigation webView %p URL: %@", webView, webView.URL);
#endif
    ((UNOWebView*)webView).lastNavigationUrl = webView.URL;
    codebrix_get_webview_navigation_finishing_callback()(webView, webView.URL.absoluteString.UTF8String);
}

- (void)webView:(WKWebView *)webView didReceiveServerRedirectForProvisionalNavigation:(null_unspecified WKNavigation *)navigation {
#if DEBUG
    NSLog(@"didReceiveServerRedirectForProvisionalNavigation webView %p", webView);
#endif
    codebrix_get_webview_navigation_starting_callback()(webView, webView.URL.absoluteString.UTF8String);
}

- (bool)isAnchorNavigation:(UNOWebView*) webView url:(NSURL*) newUrl {
    NSURL *previousUrl = webView.lastNavigationUrl;
    if (previousUrl) {
        NSArray* previousUrlParts = [previousUrl.absoluteString componentsSeparatedByString:@"#"];
        NSArray* newUrlParts = [newUrl.absoluteString componentsSeparatedByString:@"#"];
        return previousUrlParts.count > 0 && newUrlParts.count > 1 && [previousUrlParts[0] isEqual:newUrlParts[0]];
    }
    return false;
}

- (void)webView:(WKWebView *)webView decidePolicyForNavigationAction:(WKNavigationAction *)navigationAction decisionHandler:(void (^)(WKNavigationActionPolicy))decisionHandler {

    NSURL* requestUrl = navigationAction.request.URL;
    NSString *scheme = requestUrl.scheme;

    if (@available(macOS 11.3, *)) {
        // Anchor elements with the HTML5 "download" attribute.
        if (navigationAction.shouldPerformDownload) {
#if DEBUG
            NSLog(@"decidePolicyForNavigationAction webView %p URL: %@ -> Download", webView, requestUrl);
#endif
            decisionHandler(WKNavigationActionPolicyDownload);
            return;
        }
    }

    if ([requestUrl.scheme isEqualToString:@"mailto"] || [requestUrl.scheme isEqualToString:@"tel"]) {
        // launch the associated app
        [[NSWorkspace sharedWorkspace] openURL:requestUrl];
        // nothing more to do inside the webview
        decisionHandler(WKNavigationActionPolicyCancel);
    } else if ([self isAnchorNavigation: (UNOWebView*)webView url: requestUrl]) {
        const char *url = requestUrl.absoluteString.UTF8String;
        // fake starting event
        bool cancel = codebrix_get_webview_navigation_starting_callback()(webView, url);
#if DEBUG
        NSLog(@"decidePolicyForNavigationAction webView %p URL: %@ previousURL %@ -> %s", webView, requestUrl, ((UNOWebView*)webView).lastNavigationUrl, cancel ? "Cancel" : "Allow");
#endif

        decisionHandler(cancel ? WKNavigationActionPolicyCancel : WKNavigationActionPolicyAllow);
        ((UNOWebView*)webView).lastNavigationUrl = requestUrl;
        
        // fake finishing event
        codebrix_get_webview_navigation_finishing_callback()(webView, url);
    } else {
        bool isUnsupportedScheme = [scheme caseInsensitiveCompare:@"http"] != NSOrderedSame &&
        [scheme caseInsensitiveCompare:@"https"] != NSOrderedSame &&
        [scheme caseInsensitiveCompare:@"file"] != NSOrderedSame;
        if (isUnsupportedScheme) {
            const char *url = requestUrl.absoluteString.UTF8String;
            bool cancelled = codebrix_get_webview_unsupported_scheme_identified_callback()(webView, url);
#if DEBUG
            NSLog(@"decidePolicyForNavigationAction webView %p URL: %@ scheme %@ action: %s", webView, requestUrl, scheme, cancelled ? "Cancel" : "Allow");
#endif
            decisionHandler(cancelled ? WKNavigationActionPolicyCancel : WKNavigationActionPolicyAllow);
        } else {
#if DEBUG
            NSLog(@"decidePolicyForNavigationAction webView %p URL: %@ scheme %@ -> Allow", webView, requestUrl, requestUrl.scheme);
#endif
            decisionHandler(WKNavigationActionPolicyAllow);
            ((UNOWebView*)webView).lastNavigationUrl = requestUrl;
        }
    }
}

- (void)webView:(WKWebView *)webView decidePolicyForNavigationResponse:(WKNavigationResponse *)navigationResponse decisionHandler:(void (^)(WKNavigationResponsePolicy))decisionHandler {
    if (@available(macOS 11.3, *)) {
        // Turn a response the engine cannot display - or one the server marked as an
        // attachment - into a download instead of a dead-ended navigation.
        bool shouldDownload = !navigationResponse.canShowMIMEType;
        if (!shouldDownload && [navigationResponse.response isKindOfClass:NSHTTPURLResponse.class]) {
            NSString *disposition = [((NSHTTPURLResponse*)navigationResponse.response) valueForHTTPHeaderField:@"Content-Disposition"];
            shouldDownload = disposition != nil &&
                [[[disposition lowercaseString] stringByTrimmingCharactersInSet:NSCharacterSet.whitespaceCharacterSet] hasPrefix:@"attachment"];
        }
        if (shouldDownload) {
#if DEBUG
            NSLog(@"decidePolicyForNavigationResponse webView %p URL: %@ -> Download", webView, navigationResponse.response.URL);
#endif
            decisionHandler(WKNavigationResponsePolicyDownload);
            return;
        }
    }
    decisionHandler(WKNavigationResponsePolicyAllow);
}

- (void)webView:(WKWebView *)webView navigationResponse:(WKNavigationResponse *)navigationResponse didBecomeDownload:(WKDownload *)download API_AVAILABLE(macos(11.3)) {
    codebrix_download_attach(webView, download);
}

- (void)webView:(WKWebView *)webView navigationAction:(WKNavigationAction *)navigationAction didBecomeDownload:(WKDownload *)download API_AVAILABLE(macos(11.3)) {
    codebrix_download_attach(webView, download);
}

// WKScriptMessageHandler

- (void)userContentController:(WKUserContentController *)userContentController didReceiveScriptMessage:(WKScriptMessage *)message {
#if DEBUG
    NSLog(@"userContentController %p didReceiveScriptMessage: %p name %@ -> Allow", userContentController, message, message.name);
#endif
    if ([message.name isEqualToString:@"unoWebView"]) {
        if ([message.body isKindOfClass:NSString.class]) {
            const char* body = ((NSString*)message.body).UTF8String;
#if DEBUG
            NSLog(@"didReceiveScriptMessage body %s", body);
#endif
            codebrix_get_webview_receive_web_message_callback()(self, body);
        } else {
#if DEBUG
            NSLog(@"didReceiveScriptMessage (not a string) body: %@", message.body);
#endif
        }
    }
}

@synthesize originalSuperView;

@end
