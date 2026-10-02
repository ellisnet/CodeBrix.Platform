// Test-only AppKit automation. No Accessibility or Screen Recording grant is
// required: invoke the in-process menu item's accessibility action directly.
#import <AppKit/AppKit.h>

int codebrix_probe_press_menu(NSMenu *parent, const char *title)
{
    NSMenuItem *item = [parent itemWithTitle:[NSString stringWithUTF8String:title]];
    NSMenu *menu = item.submenu;
    if (!menu) { return 0; }
    __block BOOL began = NO, ended = NO, stayedOpen = NO, finished = NO;
    NSNotificationCenter *center = NSNotificationCenter.defaultCenter;
    // Menu-bar tracking notifications belong to the root menu, while the
    // submenu delegate receives menuWillOpen/menuDidClose.
    id begin = [center addObserverForName:NSMenuDidBeginTrackingNotification object:parent queue:nil
        usingBlock:^(NSNotification *note) { began = YES; }];
    id end = [center addObserverForName:NSMenuDidEndTrackingNotification object:parent queue:nil
        usingBlock:^(NSNotification *note) { ended = YES; }];
    NSPoint previous = NSEvent.mouseLocation;
    CGFloat height = NSScreen.screens.firstObject.frame.size.height;
    NSRect frame = [(id<NSAccessibility>)item accessibilityFrame];
    CGWarpMouseCursorPosition(CGPointMake(NSMidX(frame), height - NSMidY(frame)));
    NSTimer *timer = [NSTimer timerWithTimeInterval:0.4 repeats:NO block:^(NSTimer *t) {
        stayedOpen = began && !ended;
        [menu cancelTracking];
        finished = YES;
    }];
    [NSRunLoop.mainRunLoop addTimer:timer forMode:NSRunLoopCommonModes];
    // AppKit queues the menu-bar event; the return value is not its result.
    [(id<NSAccessibility>)item accessibilityPerformPress];
    NSDate *deadline = [NSDate dateWithTimeIntervalSinceNow:3];
    while (!finished && deadline.timeIntervalSinceNow > 0) {
        [NSRunLoop.mainRunLoop runMode:NSDefaultRunLoopMode beforeDate:deadline];
    }
    [timer invalidate];
    [center removeObserver:begin];
    [center removeObserver:end];
    CGWarpMouseCursorPosition(CGPointMake(previous.x, height - previous.y));
    return (began ? 1 : 0) | (stayedOpen ? 2 : 0) | (ended ? 4 : 0);
}
