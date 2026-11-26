// Generated from audits/ui-enums-unity-managed-reference.json.
export function installUiNestedEnums(owners) {
  owners["UnityEngine.EventSystems.PointerEventData"].FramePressState = Object.freeze({
    Pressed: 0,
    Released: 1,
    PressedAndReleased: 2,
    NotChanged: 3,
  });
  owners["UnityEngine.EventSystems.StandaloneInputModule"].InputMode = Object.freeze({
    Mouse: 0,
    Buttons: 1,
  });
  owners["UnityEngine.UI.CanvasScaler"].Unit = Object.freeze({
    Centimeters: 0,
    Millimeters: 1,
    Inches: 2,
    Points: 3,
    Picas: 4,
  });
  owners["UnityEngine.UI.GraphicRaycaster"].BlockingObjects = Object.freeze({
    None: 0,
    TwoD: 1,
    ThreeD: 2,
    All: 3,
  });
  owners["UnityEngine.UI.GridLayoutGroup"].Axis = Object.freeze({
    Horizontal: 0,
    Vertical: 1,
  });
  owners["UnityEngine.UI.GridLayoutGroup"].Constraint = Object.freeze({
    Flexible: 0,
    FixedColumnCount: 1,
    FixedRowCount: 2,
  });
  owners["UnityEngine.UI.GridLayoutGroup"].Corner = Object.freeze({
    UpperLeft: 0,
    UpperRight: 1,
    LowerLeft: 2,
    LowerRight: 3,
  });
  owners["UnityEngine.UI.Image"].Origin180 = Object.freeze({
    Bottom: 0,
    Left: 1,
    Top: 2,
    Right: 3,
  });
  owners["UnityEngine.UI.Image"].Origin360 = Object.freeze({
    Bottom: 0,
    Right: 1,
    Top: 2,
    Left: 3,
  });
  owners["UnityEngine.UI.Image"].Origin90 = Object.freeze({
    BottomLeft: 0,
    TopLeft: 1,
    TopRight: 2,
    BottomRight: 3,
  });
  owners["UnityEngine.UI.Image"].OriginHorizontal = Object.freeze({
    Left: 0,
    Right: 1,
  });
  owners["UnityEngine.UI.Image"].OriginVertical = Object.freeze({
    Bottom: 0,
    Top: 1,
  });
  owners["UnityEngine.UI.Scrollbar"].Direction = Object.freeze({
    LeftToRight: 0,
    RightToLeft: 1,
    BottomToTop: 2,
    TopToBottom: 3,
  });
  owners["UnityEngine.UI.Slider"].Direction = Object.freeze({
    LeftToRight: 0,
    RightToLeft: 1,
    BottomToTop: 2,
    TopToBottom: 3,
  });
  owners["UnityEngine.UI.Toggle"].ToggleTransition = Object.freeze({
    None: 0,
    Fade: 1,
  });
  owners["UnityEngine.UIElements.Columns"].StretchMode = Object.freeze({
    Grow: 0,
    GrowAndFill: 1,
  });
  owners["UnityEngine.UIElements.DropdownMenuAction"].Status = Object.freeze({
    None: 0,
    Normal: 1,
    Disabled: 2,
    Checked: 4,
    Hidden: 8,
  });
  owners["UnityEngine.UIElements.ScrollView"].NestedInteractionKind = Object.freeze({
    Default: 0,
    StopScrolling: 1,
    ForwardScrolling: 2,
  });
  owners["UnityEngine.UIElements.ScrollView"].TouchScrollBehavior = Object.freeze({
    Unrestricted: 0,
    Elastic: 1,
    Clamped: 2,
  });
  owners["UnityEngine.UIElements.UIDocument"].WorldSpaceSizeMode = Object.freeze({
    Dynamic: 0,
    Fixed: 1,
  });
  owners["UnityEngine.UIElements.VisualElement"].MeasureMode = Object.freeze({
    Undefined: 0,
    Exactly: 1,
    AtMost: 2,
  });
}

