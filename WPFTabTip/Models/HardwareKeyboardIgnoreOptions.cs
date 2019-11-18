using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFTabTipMixedHardware.Models
{
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
}
