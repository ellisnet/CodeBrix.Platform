//
//  CDBRXMediaPlayer.h
//

#pragma once

#import "PlatformNativeMac.h"
#import "CDBRXNative.h"
#import "CDBRXApplication.h"

#if DEBUG
#define DEBUG_MEDIAPLAYER   1
#endif

NS_ASSUME_NONNULL_BEGIN

@interface UNOMediaPlayer : NSObject

@property (nonatomic, strong) AVQueuePlayer* player;
@property (nonatomic, strong) AVPlayerLayer* videoLayer;

@property (nonatomic) BOOL isVideo;

-(void) setNotifications;

@end

// https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.stretch?view=winrt-26100
typedef NS_ENUM(sint32, Stretch) {
    StretchNone = 0,
    StretchFill = 1,
    StretchUniform = 2,
    StretchUniformToFill = 3,
};

typedef void (*codebrix_mediaplayer_periodic_position_update_fn_ptr)(UNOMediaPlayer* /* handle */, double position);
codebrix_mediaplayer_periodic_position_update_fn_ptr codebrix_mediaplayer_get_periodic_position_update_callback(void);

typedef void (*codebrix_mediaplayer_rate_changed_fn_ptr)(UNOMediaPlayer* /* handle */, double rate);
codebrix_mediaplayer_rate_changed_fn_ptr codebrix_mediaplayer_get_rate_changed_callback(void);

typedef void (*codebrix_mediaplayer_video_dimension_changed_fn_ptr)(UNOMediaPlayer* /* handle */, double width, double height);
codebrix_mediaplayer_video_dimension_changed_fn_ptr codebrix_mediaplayer_get_video_dimension_changed_callback(void);

typedef void (*codebrix_mediaplayer_duration_changed_fn_ptr)(UNOMediaPlayer* /* handle */, double duration);
codebrix_mediaplayer_duration_changed_fn_ptr codebrix_mediaplayer_get_duration_changed_callback(void);

typedef void (*codebrix_mediaplayer_ready_to_play_fn_ptr)(UNOMediaPlayer* /* handle */, double rate);
codebrix_mediaplayer_ready_to_play_fn_ptr codebrix_mediaplayer_get_ready_to_play_callback(void);

typedef void (*codebrix_mediaplayer_buffering_progress_changed_fn_ptr)(UNOMediaPlayer* /* handle */, double progress);
codebrix_mediaplayer_buffering_progress_changed_fn_ptr codebrix_mediaplayer_get_buffering_progress_changed_callback(void);

typedef void (*codebrix_mediaplayer_event_fn_ptr)(UNOMediaPlayer* /* handle */);
codebrix_mediaplayer_event_fn_ptr codebrix_mediaplayer_get_on_media_opened(void);
codebrix_mediaplayer_event_fn_ptr codebrix_mediaplayer_get_on_media_ended(void);
codebrix_mediaplayer_event_fn_ptr codebrix_mediaplayer_get_on_media_failed(void);
codebrix_mediaplayer_event_fn_ptr codebrix_mediaplayer_get_on_media_stalled(void);

void codebrix_mediaplayer_set_callbacks(codebrix_mediaplayer_periodic_position_update_fn_ptr periodic_position_update, codebrix_mediaplayer_rate_changed_fn_ptr rate_changed, codebrix_mediaplayer_video_dimension_changed_fn_ptr video_dimension_changed, codebrix_mediaplayer_duration_changed_fn_ptr duration_changed, codebrix_mediaplayer_ready_to_play_fn_ptr ready_to_play, codebrix_mediaplayer_buffering_progress_changed_fn_ptr buffering_progress_changed, codebrix_mediaplayer_event_fn_ptr media_opened, codebrix_mediaplayer_event_fn_ptr media_ended, codebrix_mediaplayer_event_fn_ptr media_failed, codebrix_mediaplayer_event_fn_ptr media_stalled);

id codebrix_mediaplayer_create(void);
void codebrix_mediaplayer_set_notifications(UNOMediaPlayer *media);

bool codebrix_mediaplayer_is_video(UNOMediaPlayer *media);

double codebrix_mediaplayer_get_current_time(UNOMediaPlayer *media);
void codebrix_mediaplayer_set_current_time(UNOMediaPlayer *media, double seconds);
float codebrix_mediaplayer_get_rate(UNOMediaPlayer *media);
void codebrix_mediaplayer_set_rate(UNOMediaPlayer *media, float rate);
void codebrix_mediaplayer_set_source_path(UNOMediaPlayer *media, const char *path);
void codebrix_mediaplayer_set_source_uri(UNOMediaPlayer *media, const char *uri);
void codebrix_mediaplayer_set_stretch(UNOMediaPlayer *media, Stretch stretch);
void codebrix_mediaplayer_set_volume(UNOMediaPlayer *media, float volume);

void codebrix_mediaplayer_pause(UNOMediaPlayer *media);
void codebrix_mediaplayer_play(UNOMediaPlayer *media);
void codebrix_mediaplayer_stop(UNOMediaPlayer *media);
void codebrix_mediaplayer_toggle_muted(UNOMediaPlayer *media);
void codebrix_mediaplayer_step_by(UNOMediaPlayer *media, int32_t frames);

@interface UNOMediaPlayerView : NSView<UNONativeElement>

- (nullable instancetype)initWithCoder:(NSCoder *)coder;
- (instancetype)initWithFrame:(CGRect)frame;
- (BOOL)wantsUpdateLayer;
- (void)layout;

@end

UNOMediaPlayerView* codebrix_mediaplayer_create_view(void);
void codebrix_mediaplayer_set_view(UNOMediaPlayer *media, UNOMediaPlayerView *view, NSWindow *window);

NS_ASSUME_NONNULL_END
