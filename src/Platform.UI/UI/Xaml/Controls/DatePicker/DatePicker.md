# DatePicker

## Summary

DatePicker is used to select a specific date.

* The button showing the date opens the date picker popup.
* Bind to the `Date` property of the control to set the initial date.
* The Done/OK button saves the newly selected date.

If you want to show a dimmed overlay underneath the picker, set the `DatePicker.LightDismissOverlayMode` property to `On`.

If you wish to customize the overlay color, add the following to your top-level `App.Resources`:

```xml
<SolidColorBrush x:Key="DatePickerLightDismissOverlayBackground"
                 Color="Pink" />
```
