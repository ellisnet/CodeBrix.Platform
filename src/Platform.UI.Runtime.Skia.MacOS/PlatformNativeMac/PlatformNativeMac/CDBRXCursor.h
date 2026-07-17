//
//  CDBRXCursor.h
//

#pragma once

#import "PlatformNativeMac.h"

// https://learn.microsoft.com/en-us/uwp/api/windows.ui.core.corecursortype?view=winrt-22621
typedef NS_ENUM(uint32, CoreCursorType) {
    CoreCursorTypeArrow,
    CoreCursorTypeCross,
    CoreCursorTypeCustom,
    CoreCursorTypeHand,
    CoreCursorTypeHelp,
    CoreCursorTypeIBeam,
    CoreCursorTypeSizeAll,
    CoreCursorTypeSizeNortheastSouthwest,
    CoreCursorTypeSizeNorthSouth,
    CoreCursorTypeSizeNorthwestSoutheast,
    CoreCursorTypeSizeWestEast,
    CoreCursorTypeUniversalNo,
    CoreCursorTypeUpArrow,
    CoreCursorTypeWait,
    CoreCursorTypePin,
    CoreCursorTypePerson,
};

NS_ASSUME_NONNULL_BEGIN

void codebrix_cursor_hide(void);
void codebrix_cursor_show(void);
bool codebrix_cursor_set(CoreCursorType cursorType);

// Relative mouse (mouse-look) support: while a session is active the on-screen cursor is
// frozen (CGAssociateMouseAndMouseCursorPosition) and raw motion deltas stream to the callback.
typedef void (*codebrix_mouse_relative_delta_fn_ptr)(double deltaX, double deltaY);

void codebrix_mouse_relative_begin(codebrix_mouse_relative_delta_fn_ptr callback);
void codebrix_mouse_relative_end(void);

// adapted from https://gitlab.gnome.org/GNOME/gtk/-/blob/main/gdk/macos/gdkmacoscursor.c
@interface NSCursor()
-(long long)_coreCursorType;
@end

@interface UNOCursor : NSCursor {
@private
    int type;
}

+ (instancetype)helpCursor;
+ (instancetype)sizeAllCursor;
+ (instancetype)sizeNortheastSouthwestCursor;
+ (instancetype)sizeNorthwestSoutheastCursor;
+ (instancetype)waitCursor;
@end

NS_ASSUME_NONNULL_END
