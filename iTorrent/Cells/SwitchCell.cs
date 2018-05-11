using System;

using Foundation;
using UIKit;

namespace iTorrent {
    public partial class SwitchCell : UITableViewCell {

		Action<UISwitch> action;
        
		public UISwitch GetSwitcher {
			get {
				return Switcher;
			}
		}
        
		public SwitchCell(IntPtr handle) : base(handle) {
        }

		public void SetSwitcherAction(Action<UISwitch> action) {
			this.action = action;
		}

		partial void ValueChangedAction(UISwitch sender) {
			action?.Invoke(sender);
		}
    }
}
