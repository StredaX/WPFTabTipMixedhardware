using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using WPFTabTipMixedHardware.Helpers;
using WPFTabTipMixedHardware.Models;

namespace WPFTabTipMixedHardware
{
    /// <summary>
    /// Automate TabTip diplay/closed 
    /// </summary>
    public static class TabTipAutomation
    {
        static TabTipAutomation()
        {
            if (EnvironmentEx.GetOSVersion() == OSVersion.Win7)
                return;

            TabTip.Closed += () => TabTipClosedSubject.OnNext(true);

            AutomateTabTipOpen(FocusSubject.AsObservable());
            AutomateTabTipClose(FocusSubject.AsObservable(), TabTipClosedSubject);

            AnimationHelper.ExceptionCatched += exception => ExceptionCatched?.Invoke(exception);
        }

        private static readonly Subject<Tuple<UIElement, bool>> FocusSubject = new Subject<Tuple<UIElement, bool>>();
        private static readonly Subject<bool> TabTipClosedSubject = new Subject<bool>();
        private static ManagementEventWatcher _tabTipStartWatcher;

        private static readonly List<Type> BindedUIElements = new List<Type>();

        /// <summary>
        /// By default TabTip automation happens even if keyboard is connected to device.
        /// Change IgnoreHardwareKeyboard if you want to automate
        /// TabTip only when no keyboard is connected.
        /// Default value is <seealso cref="HardwareKeyboardIgnoreOptions.IgnoreAll"/>.
        /// </summary>
        public static HardwareKeyboardIgnoreOptions IgnoreHardwareKeyboard
        {
            get { return HardwareKeyboard.IgnoreOptions; }
            set { HardwareKeyboard.IgnoreOptions = value; }
        }

        private static TabTipAutomationTrigger _automationTrigger = TabTipAutomationTrigger.OnTouch;
        /// <summary>
        /// Define triggers for the TabTip open automation.
        /// Default value is <seealso cref="TabTipAutomationTrigger.OnTouch"/>.
        /// </summary>
        public static TabTipAutomationTrigger AutomationTriggers
        {
            get => _automationTrigger;
            set
            {
                if (BindedUIElements.Any())
                    throw new NotSupportedException($"{nameof(AutomationTriggers)} must be set before first call of {nameof(BindTo)}");
                _automationTrigger = value;
            }
        }

        /// <summary>
        /// Close keyboard only if no other UIElement got focus between this waiting time
        /// </summary>
        public static TimeSpan WaitBeforeCloseKeyboard { get; set; } = TimeSpan.FromMilliseconds(100);

        private static bool _autoCloseTabTipWhenDisabled = true;

        /// <summary>
        /// Define the auto close behavior during a disabled scenari (<seealso cref="IsEnabled" /> = false)
        /// </summary>
        public static bool AutoCloseTabTipWhenDisabled
        {
            get => _autoCloseTabTipWhenDisabled;
            set
            {
                if (_autoCloseTabTipWhenDisabled != value)
                {
                    _autoCloseTabTipWhenDisabled = value;
                    if (!IsEnabled)
                    {
                        if (AutoCloseTabTipWhenDisabled)
                            StartTabTipStartWatcher();
                        else
                            StopTabTipStartWatcher();
                    }
                }
            }
        }

        private static bool _isEnabled = true;

        /// <summary>
        /// Describe the activation state of the TabTipAutomation functionnality.
        /// Default value is True
        /// </summary>
        public static bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    if (_isEnabled)
                        StopTabTipStartWatcher();
                    else if (AutoCloseTabTipWhenDisabled)
                        StartTabTipStartWatcher();
                }
            }
        }

        private static void StartTabTipStartWatcher()
        {
            _tabTipStartWatcher = new ManagementEventWatcher($"SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = '{TabTip.TabTipProcessName}.exe'");
            _tabTipStartWatcher.EventArrived += TabTibStarted;
            _tabTipStartWatcher.Start();
            TabTip.KillTapTibProcess();
        }

        private static void StopTabTipStartWatcher()
        {
            if (_tabTipStartWatcher != null)
            {
                _tabTipStartWatcher.Stop();
                _tabTipStartWatcher.Dispose();
                _tabTipStartWatcher = null;
            }
        }

        private static void TabTibStarted(object sender, EventArrivedEventArgs e)
        {
            TabTip.KillTapTibProcess();
        }

        /// <summary>
        /// Subscribe to this event if you want to know about exceptions (errors) in this library
        /// </summary>
        public static event Action<Exception> ExceptionCatched;

        /// <summary>
        /// Description of keyboards to ignore.
        /// If you want to ignore some ghost keyboard, add it's description to this list
        /// </summary>
        public static List<string> ListOfKeyboardsToIgnore => HardwareKeyboard.ListOfKeyboardsToIgnore;

        private static void AutomateTabTipClose(IObservable<Tuple<UIElement, bool>> focusObservable, Subject<bool> tabTipClosedSubject)
        {
            focusObservable
                .ObserveOn(Scheduler.Default)
                .Where(_ => IgnoreHardwareKeyboard == HardwareKeyboardIgnoreOptions.IgnoreAll || !HardwareKeyboard.IsConnectedAsync().Result)
                .Throttle(WaitBeforeCloseKeyboard) // Close only if no other UIElement got focus in `WaitBeforeCloseKeyboard` ms
                .Where(tuple => tuple.Item2 == false && !AnotherAuthorizedElementFocused())
                .Do(_ => TabTip.Close())
                .Subscribe(_ => tabTipClosedSubject.OnNext(true));

            tabTipClosedSubject
                .ObserveOnDispatcher()
                .Subscribe(_ => AnimationHelper.GetEverythingInToWorkAreaWithTabTipClosed());
        }

        private static bool AnotherAuthorizedElementFocused()
        {
            if (IsEnabled)
            {
                try
                {
                    IInputElement inputElement = null;
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        inputElement = Keyboard.FocusedElement;
                    });

                    if (inputElement is UIElement element)
                    {
                        var type = inputElement.GetType();
                        return BindedUIElements.Any(t => t == type || type.IsAssignableFrom(t) || t.IsAssignableFrom(type));
                    }
                }
                catch (Exception ex)
                {
                    ExceptionCatched?.Invoke(ex);
                }
            }

            return false;
        }

        private static void AutomateTabTipOpen(IObservable<Tuple<UIElement, bool>> focusObservable)
        {
            focusObservable
                .ObserveOn(Scheduler.Default)
                .Where(_ => IgnoreHardwareKeyboard == HardwareKeyboardIgnoreOptions.IgnoreAll || !HardwareKeyboard.IsConnectedAsync().Result)
                .Where(tuple => tuple.Item2 == true)
                .Do(_ => TabTip.OpenUndockedAndStartPoolingForClosedEvent())
                .ObserveOnDispatcher()
                .Subscribe(tuple => AnimationHelper.GetUIElementInToWorkAreaWithTabTipOpened(tuple.Item1));
        }

        private static void TouchDownRoutedEventHandler(object sender, RoutedEventArgs eventArgs)
        {
            if (sender is UIElement element && IsEnabled)
            {
                System.Diagnostics.Debug.WriteLine($"TouchDownEvent on type {element.GetType()} from {element.ToString()}");
                FocusSubject.OnNext(new Tuple<UIElement, bool>(element, true));
            }
        }

        /// <summary>
        /// Automate TabTip for given UIElement.
        /// Keyboard opens on GotFocusEvent, PreviewMouseDownEvent (i.e <seealso cref="EnableForMouseEvent"/>) or TouchDownEvent (if focused already) 
        /// and closes on LostFocusEvent.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void BindTo<T>() where T : UIElement
        {
            if (EnvironmentEx.GetOSVersion() == OSVersion.Win7)
                return;

            if (BindedUIElements.Contains(typeof(T)))
                return;

            if ((AutomationTriggers & TabTipAutomationTrigger.OnTouch) == TabTipAutomationTrigger.OnTouch)
            {
                EventManager.RegisterClassHandler(
                    classType: typeof(T),
                    routedEvent: UIElement.TouchDownEvent,
                    handler: new RoutedEventHandler(TouchDownRoutedEventHandler),
                    handledEventsToo: true);
            }

            if ((AutomationTriggers & TabTipAutomationTrigger.OnMouse) == TabTipAutomationTrigger.OnMouse)
            {
                EventManager.RegisterClassHandler(
                                classType: typeof(T),
                                routedEvent: UIElement.PreviewMouseDownEvent,
                                handler: new RoutedEventHandler((s, e) =>
                                {
                                    if (s is UIElement element && IsEnabled)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"PreviewMouseDownEvent on type {element.GetType()} from {element.GetHashCode()} Source:{e.Source} OriginalSource:{e.OriginalSource}");
                                        FocusSubject.OnNext(new Tuple<UIElement, bool>(element, true));
                                    }
                                }),
                                handledEventsToo: true);
            }

            if ((AutomationTriggers & TabTipAutomationTrigger.OnFocus) == TabTipAutomationTrigger.OnFocus)
            {
                EventManager.RegisterClassHandler(
                                classType: typeof(T),
                                routedEvent: UIElement.GotFocusEvent,
                                handler: new RoutedEventHandler((s, e) =>
                                {
                                    if (s is UIElement element && IsEnabled)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"GotFocusEvent on type {element.GetType()} from {element.ToString()}");
                                        FocusSubject.OnNext(new Tuple<UIElement, bool>(element, true));
                                    }
                                }),
                                handledEventsToo: true);
            }

            EventManager.RegisterClassHandler(
                classType: typeof(T),
                routedEvent: UIElement.LostFocusEvent,
                handler: new RoutedEventHandler((s, e) =>
                {
                    if (s is UIElement element && IsEnabled)
                    {
                        System.Diagnostics.Debug.WriteLine($"LostFocusEvent on type {element.GetType()} from {element.ToString()}");
                        FocusSubject.OnNext(new Tuple<UIElement, bool>(element, false));
                    }
                }),
                handledEventsToo: true);

            BindedUIElements.Add(typeof(T));
        }
    }
}
