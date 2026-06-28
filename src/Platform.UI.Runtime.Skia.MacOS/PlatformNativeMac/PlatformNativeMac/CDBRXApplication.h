//
//  CDBRXApplication.h
//

#pragma once

#import "PlatformNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

typedef void (*system_theme_change_fn_ptr)(void);
system_theme_change_fn_ptr codebrix_get_system_theme_change_callback(void);
void codebrix_set_system_theme_change_callback(system_theme_change_fn_ptr p);
uint32 codebrix_get_system_theme(void);

bool codebrix_app_initialize(bool *supportsMetal);
NSWindow* codebrix_app_get_main_window(void);

id<MTLDevice> codebrix_application_get_metal_device(void);
void codebrix_application_set_badge(const char *badge);
void codebrix_application_set_icon(const char *path);
bool codebrix_application_open_url(const char *url);
bool codebrix_application_query_url_support(const char *url);
bool codebrix_application_is_bundled(void);

typedef void (*application_start_fn_ptr)(void);
application_start_fn_ptr codebrix_get_application_start_callback(void);

typedef bool (*application_can_exit_fn_ptr)(void);
application_can_exit_fn_ptr codebrix_get_application_can_exit_callback(void);
void codebrix_set_application_can_exit_callback(application_can_exit_fn_ptr p);
void codebrix_application_quit(void);

@interface UNOApplicationDelegate : NSObject <NSApplicationDelegate>

- (void)applicationDidFinishLaunching:(NSNotification *)notification;
- (NSApplicationTerminateReply)applicationShouldTerminate:(NSApplication *)sender;
- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)sender;
- (void)observeValueForKeyPath:(nullable NSString *)keyPath ofObject:(nullable id)object change:(nullable NSDictionary<NSKeyValueChangeKey, id> *)change context:(nullable void *)context;

@end

NS_ASSUME_NONNULL_END
