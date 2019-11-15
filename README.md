# WPFTabTipMixedhardware
Simple TabTip / Virtual Keyboard integration for WPF apps with touchscreen and/or keyboard.
Available via [nuget](https://www.nuget.org/packages/WPFTabTipMixedHardware/) ![Nuget](https://img.shields.io/nuget/dt/WPFTabTipMixedHarware)

## Package

![Nuget](https://img.shields.io/nuget/v/WPFTabTipMixedHardware)

## Getting started

You can bind TabTip automation logic to any `UIElement`. Virtual Keyboard will open when any such element will get touched (and mouse event with parameters), and it will close when element will lose focus. Not only that, but `TabTipAutomation` will move `UIElement` (or `Window`) into  view, so that TabTip will not block focused element.

### Hardware keyboard detection and mouse event

By default TabTip automation will occur only on touch, independently with hardware keyboard plugged.
Default behavior can be change with two properties :

- `TabTipAutomation.EnableForMouseEvent` : Will trigger open virtual keaybord on MouseClick event and Focus event
- `TabTipAutomation.IgnoreHardwareKeyboard` : You can change the hardware keyboard detection with the following values :

```c#
public enum HardwareKeyboardIgnoreOptions
    {
        /// <summary>
        /// Do not ignore any keyboard.
        /// </summary>
        DoNotIgnore,

        /// <summary>
        /// Ignore keyboard, if there is only one, and it's description 
        /// can be found in ListOfKeyboardsToIgnore.
        /// </summary>
        IgnoreIfSingleInstanceOnList,

        /// <summary>
        /// Ignore keyboard, if there is only one.
        /// </summary>
        IgnoreIfSingleInstance,
        
        /// <summary>
        /// Ignore all keyboards for which the description 
        /// can be found in ListOfKeyboardsToIgnore
        /// </summary>
        IgnoreIfOnList,

        /// <summary>
        /// Ignore all keyboards
        /// </summary>
        IgnoreAll
    }
```

If you want to ignore specific keyboard you should set `TabTipAutomation.IgnoreHardwareKeyboard` to either `IgnoreIfSingleInstanceOnList` or `IgnoreIfOnList`, and add keyboard description to `TabTipAutomation.ListOfKeyboardsToIgnore`.

To get description of keyboards connected to machine you can use following code:

```c#
new ManagementObjectSearcher(new SelectQuery("Win32_Keyboard")).Get()
                .Cast<ManagementBaseObject>()
                .SelectMany(keyboard =>
                    keyboard.Properties
                        .Cast<PropertyData>()
                        .Where(k => k.Name == "Description")
                        .Select(k => k.Value as string))
                .ToList();
```

### Change keyboard layout

To specify keyboard layout to be used with certain element you can set `InputScope` property in xaml to one of the following:
- Default
- Url
- EmailSmtpAddress
- Number

## Test
You can test the behaviors with the included test application. Set UITest project as 'Set as startup projet' and run. 
