//
//  CDBRXClipboard.h
//

#pragma once

#import "PlatformNativeMac.h"

NS_ASSUME_NONNULL_BEGIN

struct ClipboardData {
    char* htmlContent;
    char* rtfContent;
    char* textContent;
    char* uri;
    char* fileUrl;

    char* bitmapFormat;
    char* bitmapPath;
    void* bitmapData;
    uint64 bitmapSize;
};

void codebrix_clipboard_clear(void);

void codebrix_clipboard_get_content(struct ClipboardData* data);
bool codebrix_clipboard_set_content(struct ClipboardData* data);

void codebrix_clipboard_start_content_changed(void);
void codebrix_clipboard_stop_content_changed(void);

typedef void (*clipboard_changed_fn_ptr)(void);
clipboard_changed_fn_ptr codebrix_clipboard_get_content_changed_callback(void);
void codebrix_clipboard_set_content_changed_callback(clipboard_changed_fn_ptr p);

NS_ASSUME_NONNULL_END
