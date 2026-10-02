// Opt-in NSMenu projection for the Skia macOS host (Intel and Apple Silicon).
#import <AppKit/AppKit.h>

typedef void (*codebrix_menu_callback)(int64_t context, int64_t item, int32_t event);
static codebrix_menu_callback menuCallback;

@class CDBRXSystemMenu;
@interface CDBRXProjectedMenu : NSMenu
@property(nonatomic) int64_t nodeId;
@property(nonatomic, strong) NSMutableDictionary<NSNumber *, NSMenuItem *> *projectedItems;
@end
@implementation CDBRXProjectedMenu
- (instancetype)initWithTitle:(NSString *)title
{
    if ((self = [super initWithTitle:title])) { _projectedItems = [NSMutableDictionary new]; }
    return self;
}
@end

@interface CDBRXSystemMenu : NSObject <NSMenuDelegate>
@property(nonatomic, weak) NSWindow *window;
@property(nonatomic) int64_t context;
@property(nonatomic) BOOL visible;
@property(nonatomic, strong) CDBRXProjectedMenu *root;
@property(nonatomic, strong) NSMenu *services;
@property(nonatomic, strong) NSMutableDictionary<NSNumber *, CDBRXProjectedMenu *> *menus;
@property(nonatomic, strong) id activationObserver;
- (void)setItems:(NSArray *)items inMenu:(CDBRXProjectedMenu *)menu;
@end

static NSMutableArray<CDBRXSystemMenu *> *systemMenus;
static NSMenu *originalMenu;
static NSMenu *originalServicesMenu;
static NSMenu *originalHelpMenu;
static NSMenu *unlistedHelpMenu;

static void selectSystemMenu(void)
{
    // A console-launched app can have a visible window before AppKit assigns
    // key/main status (and while another application is active).
    NSWindow *window = NSApp.keyWindow ?: NSApp.mainWindow ?: NSApp.orderedWindows.firstObject;
    for (CDBRXSystemMenu *menu in systemMenus) {
        if (menu.visible && menu.window == window) {
            // A Help menu titled by the application otherwise gains AppKit's
            // Spotlight search UI automatically. An unlisted helpMenu is the
            // documented opt-out: project only the application's XAML items.
            if (NSApp.helpMenu != unlistedHelpMenu) { NSApp.helpMenu = unlistedHelpMenu; }
            if (NSApp.mainMenu != menu.root) { NSApp.mainMenu = menu.root; }
            if (NSApp.servicesMenu != menu.services) { NSApp.servicesMenu = menu.services; }
            return;
        }
    }
    if (NSApp.mainMenu != originalMenu) { NSApp.mainMenu = originalMenu; }
    if (NSApp.servicesMenu != originalServicesMenu) { NSApp.servicesMenu = originalServicesMenu; }
    if (NSApp.helpMenu != originalHelpMenu) { NSApp.helpMenu = originalHelpMenu; }
}

@implementation CDBRXSystemMenu
- (void)invokeItem:(NSMenuItem *)item
{
    if (menuCallback) {
        menuCallback(self.context, item.tag, NSApp.currentEvent.type == NSEventTypeKeyDown ? 4 : 2);
    }
}

- (void)menuNeedsUpdate:(NSMenu *)menu
{
    if (menuCallback && [(CDBRXProjectedMenu *)menu nodeId] != 0) {
        menuCallback(self.context, [(CDBRXProjectedMenu *)menu nodeId], 0);
    }
}

- (void)menuWillOpen:(NSMenu *)menu
{
    if (menuCallback) { menuCallback(self.context, [(CDBRXProjectedMenu *)menu nodeId], 3); }
}

- (void)menuDidClose:(NSMenu *)menu
{
    if (menuCallback) { menuCallback(self.context, [(CDBRXProjectedMenu *)menu nodeId], 1); }
}

- (void)forgetSubmenus:(NSMenu *)menu
{
    for (NSMenuItem *item in menu.itemArray) {
        if ([item.submenu isKindOfClass:CDBRXProjectedMenu.class]) {
            [self forgetSubmenus:item.submenu];
            NSNumber *node = @([(CDBRXProjectedMenu *)item.submenu nodeId]);
            if (self.menus[node] == item.submenu) { [self.menus removeObjectForKey:node]; }
        }
    }
}

- (void)setItems:(NSArray *)items inMenu:(CDBRXProjectedMenu *)menu
{
    // AppKit keeps state on the actual NSMenu/NSMenuItem instances, especially
    // for Spotlight's Help menu. Replacing the tree during menuNeedsUpdate:
    // invalidates that state. Reconcile by the managed element's stable ID and
    // leave items owned by AppKit (including its search field) untouched.
    NSMutableSet<NSNumber *> *live = [NSMutableSet new];
    for (NSDictionary *entry in items) { [live addObject:entry[@"Id"]]; }
    for (NSNumber *node in menu.projectedItems.allKeys) {
        if (![live containsObject:node]) {
            NSMenuItem *item = menu.projectedItems[node];
            if (item.submenu) {
                [self forgetSubmenus:item.submenu];
                if (self.menus[node] == item.submenu) { [self.menus removeObjectForKey:node]; }
            }
            [menu removeItem:item];
            [menu.projectedItems removeObjectForKey:node];
        }
    }
    NSInteger position = 0;
    for (NSDictionary *entry in items) {
        NSNumber *node = entry[@"Id"];
        NSMenuItem *item = menu.projectedItems[node];
        if (!item) {
            item = [entry[@"Separator"] boolValue] ? NSMenuItem.separatorItem
                : [[NSMenuItem alloc] initWithTitle:@"" action:nil keyEquivalent:@""];
            item.tag = node.longLongValue;
            menu.projectedItems[node] = item;
        }
        if (!item.isSeparatorItem) {
            NSString *title = entry[@"Title"] ?: @"";
            if (![item.title isEqualToString:title]) { item.title = title; }
            item.keyEquivalent = entry[@"Key"] ?: @"";
            item.action = @selector(invokeItem:);
            item.target = self;
            item.enabled = [entry[@"Enabled"] boolValue];
            item.state = [entry[@"Checked"] boolValue] ? NSControlStateValueOn : NSControlStateValueOff;
            int modifiers = [entry[@"Modifiers"] intValue]; // Windows.System.VirtualKeyModifiers
            item.keyEquivalentModifierMask = ((modifiers & 1) ? NSEventModifierFlagControl : 0)
                | ((modifiers & 2) ? NSEventModifierFlagOption : 0)
                | ((modifiers & 4) ? NSEventModifierFlagShift : 0)
                | ((modifiers & 8) ? NSEventModifierFlagCommand : 0);
        }
        if ([entry[@"Children"] isKindOfClass:NSArray.class]) {
            CDBRXProjectedMenu *submenu = (CDBRXProjectedMenu *)item.submenu;
            if (!submenu) {
                submenu = [[CDBRXProjectedMenu alloc] initWithTitle:item.title];
                submenu.nodeId = item.tag;
                submenu.delegate = self;
                submenu.autoenablesItems = NO;
                self.menus[node] = submenu;
                item.submenu = submenu;
            }
            if (![submenu.title isEqualToString:item.title]) { submenu.title = item.title; }
            item.action = nil;
            [self setItems:entry[@"Children"] inMenu:submenu];
        }
        // Skip host/system items; retain their order and positions relative to
        // the surrounding projected items. Do not detach unchanged items.
        while (position < menu.numberOfItems) {
            NSMenuItem *current = [menu itemAtIndex:position];
            if (menu.projectedItems[@(current.tag)] == current) { break; }
            position++;
        }
        if ([menu indexOfItem:item] != position) {
            if (item.menu == menu) { [menu removeItem:item]; }
            [menu insertItem:item atIndex:position];
        }
        position++;
    }
}
@end

void codebrix_menu_set_callback(codebrix_menu_callback callback)
{
    menuCallback = callback;
}

void *codebrix_menu_create(NSWindow *window, int64_t context, const char *title)
{
    if (!systemMenus) {
        systemMenus = [NSMutableArray new];
        originalMenu = NSApp.mainMenu;
        originalServicesMenu = NSApp.servicesMenu;
        originalHelpMenu = NSApp.helpMenu;
        unlistedHelpMenu = [[NSMenu alloc] initWithTitle:@""];
    }
    CDBRXSystemMenu *controller = [CDBRXSystemMenu new];
    controller.window = window;
    controller.context = context;
    controller.visible = YES;
    controller.menus = [NSMutableDictionary new];
    controller.root = [[CDBRXProjectedMenu alloc] initWithTitle:@""];
    controller.root.autoenablesItems = NO;
    controller.menus[@0] = controller.root;
    NSString *name = title ? [NSString stringWithUTF8String:title] : NSProcessInfo.processInfo.processName;
    if (name.length == 0) { name = NSProcessInfo.processInfo.processName; }
    NSMenuItem *applicationItem = [[NSMenuItem alloc] initWithTitle:name action:nil keyEquivalent:@""];
    NSMenu *applicationMenu = [[NSMenu alloc] initWithTitle:name];
    applicationItem.submenu = applicationMenu;
    [controller.root addItem:applicationItem];
    NSMenuItem *services = [[NSMenuItem alloc] initWithTitle:@"Services" action:nil keyEquivalent:@""];
    services.submenu = [[NSMenu alloc] initWithTitle:@"Services"];
    [applicationMenu addItem:services];
    controller.services = services.submenu;
    [applicationMenu addItem:NSMenuItem.separatorItem];
    [applicationMenu addItemWithTitle:[@"Hide " stringByAppendingString:name] action:@selector(hide:) keyEquivalent:@"h"];
    NSMenuItem *hideOthers = [applicationMenu addItemWithTitle:@"Hide Others" action:@selector(hideOtherApplications:) keyEquivalent:@"h"];
    hideOthers.keyEquivalentModifierMask = NSEventModifierFlagCommand | NSEventModifierFlagOption;
    [applicationMenu addItemWithTitle:@"Show All" action:@selector(unhideAllApplications:) keyEquivalent:@""];
    // Preserve application command locations and handlers. In particular, do
    // not invent a Quit action that bypasses the app's unsaved-document check.
    __weak CDBRXSystemMenu *weakController = controller;
    controller.activationObserver = [NSNotificationCenter.defaultCenter
        addObserverForName:NSWindowDidBecomeKeyNotification object:nil queue:nil
        usingBlock:^(NSNotification *notification) {
            if (weakController) { selectSystemMenu(); }
        }];
    [systemMenus addObject:controller];
    selectSystemMenu();
    return (__bridge_retained void *)controller;
}

void codebrix_menu_set_items(void *handle, int64_t parent, const char *json)
{
    CDBRXSystemMenu *controller = (__bridge CDBRXSystemMenu *)handle;
    CDBRXProjectedMenu *menu = controller.menus[@(parent)];
    if (!menu || !json) { return; }
    NSData *data = [[NSString stringWithUTF8String:json] dataUsingEncoding:NSUTF8StringEncoding];
    NSArray *entries = [NSJSONSerialization JSONObjectWithData:data options:0 error:NULL];
    if ([entries isKindOfClass:NSArray.class]) { [controller setItems:entries inMenu:menu]; }
}

void codebrix_menu_set_visible(void *handle, int32_t visible)
{
    CDBRXSystemMenu *controller = (__bridge CDBRXSystemMenu *)handle;
    controller.visible = visible != 0;
    selectSystemMenu();
}

void codebrix_menu_destroy(void *handle)
{
    CDBRXSystemMenu *controller = (__bridge_transfer CDBRXSystemMenu *)handle;
    [NSNotificationCenter.defaultCenter removeObserver:controller.activationObserver];
    [systemMenus removeObject:controller];
    selectSystemMenu();
}
