using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WALLnutClient
{
    public interface IControlView
    {
        bool ViewIsReadyToAppear { get; }
        bool ViewIsVisible { get; }
        event EventHandler ViewIsReadyToAppearChanged;
        event EventHandler ViewIsVisibleChanged;
        event EventHandler BeforeViewDisappear;
        event EventHandler AfterViewDisappear;
        void SetViewIsVisible(bool v);
        void RaiseBeforeViewDisappear();
        void RaiseAfterViewDisappear();
    }
}
