// Browser-backed runtime UI declarations for variation source compilation.
// Unity project transpilation resolves the installed Unity assemblies instead.
namespace UnityEngine
{
    public class Font : Object { }
    public enum FontStyle { Normal = 0, Bold = 1, Italic = 2, BoldAndItalic = 3 }
    public enum HorizontalWrapMode { Wrap = 0, Overflow = 1 }
    public enum VerticalWrapMode { Truncate = 0, Overflow = 1 }
    public enum RenderMode { ScreenSpaceOverlay = 0, ScreenSpaceCamera = 1, WorldSpace = 2 }
    public enum TextAnchor { UpperLeft = 0, UpperCenter = 1, UpperRight = 2,
        MiddleLeft = 3, MiddleCenter = 4, MiddleRight = 5,
        LowerLeft = 6, LowerCenter = 7, LowerRight = 8 }
    public class Canvas : Behaviour
    {
        public delegate void WillRenderCanvases();
        public static event WillRenderCanvases preWillRenderCanvases { add { } remove { } }
        public static event WillRenderCanvases willRenderCanvases { add { } remove { } }
        public static void ForceUpdateCanvases() { }
        public static Material GetDefaultCanvasMaterial() => null!;
        public static Material GetDefaultCanvasTextMaterial() => null!;
        public static Material GetETC1SupportedCanvasMaterial() => null!;
        public AdditionalCanvasShaderChannels additionalShaderChannels { get; set; }
        public int cachedSortingLayerValue { get; }
        public float normalizedSortingGridSize { get; }
        public int renderOrder { get; }
        public float sortingGridNormalizedSize { get; set; }
        public bool updateRectTransformForStandalone { get; set; }
        public bool vertexColorAlwaysGammaSpace { get; set; }
        public int sortingOrder { get; set; }
        public int sortingLayerID { get; set; }
        public string sortingLayerName { get; set; } = "Default";
        public bool overrideSorting { get; set; }
        public int targetDisplay { get; set; }
        public RenderMode renderMode { get; set; }
        public float planeDistance { get; set; }
        public Camera worldCamera { get; set; } = null!;
        public float scaleFactor { get; set; }
        public float referencePixelsPerUnit { get; set; } = 100;
        public bool pixelPerfect { get; set; }
        public bool overridePixelPerfect { get; set; }
        public Canvas rootCanvas { get; }
        public bool isRootCanvas { get; }
        public Rect pixelRect { get; }
        public Vector2 renderingDisplaySize { get; }
    }

    public sealed class CanvasRenderer : Component
    {
        public delegate void OnRequestRebuild();
        public event OnRequestRebuild onRequestRebuild { add { } remove { } }
        public int absoluteDepth { get; }
        public Vector2 clippingSoftness { get; set; }
        public bool cullTransparentMesh { get; set; }
        public bool hasMoved { get; }
        public bool hasRectClipping { get; }
        public bool isMask { get; set; }
        public int relativeDepth { get; }
        public void EnableRectClipping(Rect rect) { }
        public void DisableRectClipping() { }
        public float GetInheritedAlpha() => 1;
        public bool cull { get; set; }
        public Color GetColor() => Color.white;
        public void SetColor(Color color) { }
        public float GetAlpha() => 1;
        public void SetAlpha(float alpha) { }
        public void Clear() { }
        public void SetMesh(Mesh mesh) { }
    }

    public interface ICanvasRaycastFilter
    {
        bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera);
    }

    public class CanvasGroup : Behaviour, ICanvasRaycastFilter
    {
        public float alpha { get; set; } = 1;
        public bool interactable { get; set; } = true;
        public bool blocksRaycasts { get; set; } = true;
        public bool ignoreParentGroups { get; set; }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) => blocksRaycasts;
    }

    public static class RectTransformUtility
    {
        public static Bounds CalculateRelativeRectTransformBounds(Transform child) => default;
        public static Bounds CalculateRelativeRectTransformBounds(Transform root, Transform child) => default;
        public static void FlipLayoutAxes(RectTransform rect, bool keepPositioning, bool recursive) { }
        public static void FlipLayoutOnAxis(RectTransform rect, int axis, bool keepPositioning, bool recursive) { }
        public static Vector2 PixelAdjustPoint(Vector2 point, Transform transform, Canvas canvas) => point;
        public static Rect PixelAdjustRect(RectTransform rect, Canvas canvas) => rect.rect;
        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 point) => false;
        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 point, Camera camera) => false;
        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 point, Camera camera,
            Vector4 offset) => false;
        public static bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 point,
            Camera camera, out Vector2 local) { local = default; return false; }
        public static Ray ScreenPointToRay(Camera camera, Vector2 point) => default;
        public static bool ScreenPointToWorldPointInRectangle(RectTransform rect, Vector2 point,
            Camera camera, out Vector3 world) { world = default; return false; }
        public static Vector2 WorldToScreenPoint(Camera camera, Vector3 world) => default;
    }

    public static class UISystemProfilerApi
    {
        public enum SampleType { Layout = 0, Render = 1 }
        public static void AddMarker(string name, Object context) { }
        public static void BeginSample(SampleType type) { }
        public static void EndSample(SampleType type) { }
    }
}

namespace UnityEngine.UI
{
    public interface ICanvasElement
    {
        UnityEngine.Transform transform { get; }
        void Rebuild(CanvasUpdate executing);
        void LayoutComplete();
        void GraphicUpdateComplete();
        bool IsDestroyed();
    }
    public interface IClipper { void PerformClipping(); }
    public interface ILayoutElement
    {
        void CalculateLayoutInputHorizontal();
        void CalculateLayoutInputVertical();
        float minWidth { get; }
        float minHeight { get; }
        float preferredWidth { get; }
        float preferredHeight { get; }
        float flexibleWidth { get; }
        float flexibleHeight { get; }
        int layoutPriority { get; }
    }
    public interface ILayoutController
    {
        void SetLayoutHorizontal();
        void SetLayoutVertical();
    }
    public interface ILayoutGroup : ILayoutController { }
    public interface ILayoutSelfController : ILayoutController { }
    public interface ILayoutIgnorer { bool ignoreLayout { get; } }
    public interface IMaskable { void RecalculateMasking(); }
    public interface IMask
    {
        UnityEngine.RectTransform rectTransform { get; }
        bool Enabled();
    }
    public interface IMaterialModifier
    {
        UnityEngine.Material GetModifiedMaterial(UnityEngine.Material baseMaterial);
    }
    public static class Clipping
    {
        public static UnityEngine.Rect FindCullAndClipWorldRect(
            System.Collections.Generic.List<RectMask2D> rectMaskParents, out bool validRect)
        { validRect = false; return default; }
    }
    public static class DefaultControls
    {
        public interface IFactoryControls
        {
            UnityEngine.GameObject CreateGameObject(string name, params System.Type[] components);
        }
        public struct Resources
        {
            public UnityEngine.Sprite standard, background, inputField, knob;
            public UnityEngine.Sprite checkmark, dropdown, mask;
        }
        public static IFactoryControls factory { get; }
        public static UnityEngine.GameObject CreatePanel(Resources resources) => null!;
        public static UnityEngine.GameObject CreateButton(Resources resources) => null!;
        public static UnityEngine.GameObject CreateText(Resources resources) => null!;
        public static UnityEngine.GameObject CreateImage(Resources resources) => null!;
        public static UnityEngine.GameObject CreateRawImage(Resources resources) => null!;
        public static UnityEngine.GameObject CreateSlider(Resources resources) => null!;
        public static UnityEngine.GameObject CreateScrollbar(Resources resources) => null!;
        public static UnityEngine.GameObject CreateToggle(Resources resources) => null!;
        public static UnityEngine.GameObject CreateInputField(Resources resources) => null!;
        public static UnityEngine.GameObject CreateDropdown(Resources resources) => null!;
        public static UnityEngine.GameObject CreateScrollView(Resources resources) => null!;
    }
    public static class FontUpdateTracker
    {
        public static void TrackText(Text text) { }
        public static void UntrackText(Text text) { }
    }
    public static class GraphicRebuildTracker
    {
        public static void TrackGraphic(Graphic graphic) { }
        public static void UnTrackGraphic(Graphic graphic) { }
        public static void DisableTrackGraphic(Graphic graphic) { }
    }
    public interface IClippable
    {
        UnityEngine.GameObject gameObject { get; }
        UnityEngine.RectTransform rectTransform { get; }
        void RecalculateClipping();
        void Cull(UnityEngine.Rect clipRect, bool validRect);
        void SetClipRect(UnityEngine.Rect clipRect, bool validRect);
        void SetClipSoftness(UnityEngine.Vector2 softness);
    }
    public struct ColorBlock
    {
        public UnityEngine.Color normalColor { get; set; }
        public UnityEngine.Color highlightedColor { get; set; }
        public UnityEngine.Color pressedColor { get; set; }
        public UnityEngine.Color selectedColor { get; set; }
        public UnityEngine.Color disabledColor { get; set; }
        public float colorMultiplier { get; set; }
        public float fadeDuration { get; set; }
        public static ColorBlock defaultColorBlock => new();
        public bool Equals(ColorBlock other) => false;
        public override bool Equals(object obj) => false;
        public override int GetHashCode() => 0;
    }
    public class AnimationTriggers
    {
        public string normalTrigger { get; set; } = "Normal";
        public string highlightedTrigger { get; set; } = "Highlighted";
        public string pressedTrigger { get; set; } = "Pressed";
        public string selectedTrigger { get; set; } = "Selected";
        public string disabledTrigger { get; set; } = "Disabled";
    }
    public struct SpriteState
    {
        public UnityEngine.Sprite highlightedSprite { get; set; }
        public UnityEngine.Sprite pressedSprite { get; set; }
        public UnityEngine.Sprite selectedSprite { get; set; }
        public UnityEngine.Sprite disabledSprite { get; set; }
        public bool Equals(SpriteState other) => false;
    }
    public struct Navigation
    {
        public enum Mode { None = 0, Horizontal = 1, Vertical = 2, Automatic = 3, Explicit = 4 }
        public Mode mode { get; set; }
        public bool wrapAround { get; set; }
        public Selectable selectOnUp { get; set; }
        public Selectable selectOnDown { get; set; }
        public Selectable selectOnLeft { get; set; }
        public Selectable selectOnRight { get; set; }
        public static Navigation defaultNavigation => new() { mode = Mode.Automatic };
        public bool Equals(Navigation other) => false;
    }

    public partial class GraphicRaycaster : UnityEngine.EventSystems.BaseRaycaster
    {
        public bool ignoreReversedGraphics { get; set; } = true;
        public int blockingObjects { get; set; }
        public UnityEngine.LayerMask blockingMask { get; set; }
        public override void Raycast(UnityEngine.EventSystems.PointerEventData eventData,
            System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> resultAppendList) { }
    }

    public class Mask : UnityEngine.Behaviour
    {
        public bool showMaskGraphic { get; set; } = true;
        public Graphic graphic { get; }
        public UnityEngine.RectTransform rectTransform { get; }
        public bool MaskEnabled() => false;
        public void OnSiblingGraphicEnabledDisabled() { }
        public bool IsRaycastLocationValid(UnityEngine.Vector2 point,
            UnityEngine.Camera camera) => true;
        public UnityEngine.Material GetModifiedMaterial(UnityEngine.Material baseMaterial)
            => baseMaterial;
    }

    public class RectMask2D : UnityEngine.Behaviour, IClipper
    {
        public UnityEngine.Vector4 padding { get; set; }
        public UnityEngine.Vector2Int softness { get; set; }
        public UnityEngine.RectTransform rectTransform { get; }
        public UnityEngine.Rect canvasRect { get; }
        public bool IsRaycastLocationValid(UnityEngine.Vector2 point,
            UnityEngine.Camera camera) => true;
        public void AddClippable(IClippable clippable) { }
        public void RemoveClippable(IClippable clippable) { }
        public void PerformClipping() { }
        public void UpdateClipSoftness() { }
    }
    public class MaskUtilities
    {
        public static UnityEngine.Transform FindRootSortOverrideCanvas(
            UnityEngine.Transform start) => null!;
        public static int GetStencilDepth(UnityEngine.Transform transform,
            UnityEngine.Transform stopAfter) => 0;
        public static bool IsDescendantOrSelf(UnityEngine.Transform father,
            UnityEngine.Transform child) => false;
        public static RectMask2D GetRectMaskForClippable(IClippable clippable) => null!;
        public static void GetRectMasksForClip(RectMask2D clipper,
            System.Collections.Generic.List<RectMask2D> masks) { }
        public static void Notify2DMaskStateChanged(UnityEngine.Component mask) { }
        public static void NotifyStencilStateChanged(UnityEngine.Component mask) { }
    }

    public partial class CanvasScaler : UnityEngine.Behaviour
    {
        public enum ScaleMode { ConstantPixelSize = 0, ScaleWithScreenSize = 1, ConstantPhysicalSize = 2 }
        public enum ScreenMatchMode { MatchWidthOrHeight = 0, Expand = 1, Shrink = 2 }
        public ScaleMode uiScaleMode { get; set; }
        public float scaleFactor { get; set; } = 1;
        public float referencePixelsPerUnit { get; set; } = 100;
        public UnityEngine.Vector2 referenceResolution { get; set; } = new(800, 600);
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
        public float dynamicPixelsPerUnit { get; set; } = 1;
        public Unit physicalUnit { get; set; } = Unit.Points;
        public float fallbackScreenDPI { get; set; } = 96;
        public float defaultSpriteDPI { get; set; } = 96;
    }

    public class Selectable : UnityEngine.Behaviour
    {
        public enum Transition { None = 0, ColorTint = 1, SpriteSwap = 2, Animation = 3 }
        public static Selectable[] allSelectablesArray => System.Array.Empty<Selectable>();
        public static System.Collections.Generic.List<Selectable> allSelectables => new();
        public static int allSelectableCount => 0;
        public static int AllSelectablesNoAlloc(Selectable[] selectables) => 0;
        public bool interactable { get; set; } = true;
        public Navigation navigation { get; set; } = Navigation.defaultNavigation;
        public Transition transition { get; set; } = Transition.ColorTint;
        public ColorBlock colors { get; set; } = ColorBlock.defaultColorBlock;
        public AnimationTriggers animationTriggers { get; set; } = new();
        public SpriteState spriteState { get; set; }
        public Graphic targetGraphic { get; set; }
        public Image image { get; set; }
        public UnityEngine.Animator animator { get; }
        public bool IsInteractable() => interactable;
        public bool IsDestroyed() => false;
        public void Select() { }
        public Selectable FindSelectable(UnityEngine.Vector3 direction) => null;
        public virtual Selectable FindSelectableOnLeft() => null;
        public virtual Selectable FindSelectableOnRight() => null;
        public virtual Selectable FindSelectableOnUp() => null;
        public virtual Selectable FindSelectableOnDown() => null;
        public virtual void OnMove(UnityEngine.EventSystems.AxisEventData eventData) { }
        public virtual void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData) { }
        public virtual void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData) { }
    }

    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEngine.Events.UnityEvent { }
        public ButtonClickedEvent onClick { get; set; } = new();
    }

    public class Dropdown : Selectable
    {
        public class OptionData
        {
            public OptionData() { }
            public OptionData(string text) { }
            public OptionData(UnityEngine.Sprite image) { }
            public OptionData(string text, UnityEngine.Sprite image) { }
            public string text { get; set; }
            public UnityEngine.Sprite image { get; set; }
        }
        public class OptionDataList
        {
            public System.Collections.Generic.List<OptionData> options { get; set; } = new();
        }
        public class DropdownEvent : UnityEngine.Events.UnityEvent<int> { }
        public UnityEngine.RectTransform template { get; set; }
        public Text captionText { get; set; }
        public Image captionImage { get; set; }
        public Text itemText { get; set; }
        public Image itemImage { get; set; }
        public System.Collections.Generic.List<OptionData> options { get; set; }
        public int value { get; set; }
        public DropdownEvent onValueChanged { get; set; }
        public float alphaFadeSpeed { get; set; } = 0.15f;
        public void SetValueWithoutNotify(int value) { }
        public void AddOptions(System.Collections.Generic.List<string> options) { }
        public void AddOptions(System.Collections.Generic.List<OptionData> options) { }
        public void ClearOptions() { }
        public void RefreshShownValue() { }
        public void Show() { }
        public void Hide() { }
        public void OnCancel(UnityEngine.EventSystems.BaseEventData eventData) { }
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData) { }
        public void OnSubmit(UnityEngine.EventSystems.BaseEventData eventData) { }
    }

    public partial class Toggle : Selectable, ICanvasElement
    {
        public class ToggleEvent : UnityEngine.Events.UnityEvent<bool> { }
        public ToggleTransition toggleTransition { get; set; } = ToggleTransition.Fade;
        public ToggleGroup group { get; set; }
        public bool isOn { get; set; }
        public ToggleEvent onValueChanged { get; set; } = new();
        public void SetIsOnWithoutNotify(bool value) { }
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData) { }
        public void OnSubmit(UnityEngine.EventSystems.BaseEventData eventData) { }
        public void Rebuild(CanvasUpdate update) { }
        public void LayoutComplete() { }
        public void GraphicUpdateComplete() { }
    }
    public class ToggleGroup : UnityEngine.EventSystems.UIBehaviour
    {
        protected ToggleGroup() { }
        public bool allowSwitchOff { get; set; }
        public void RegisterToggle(Toggle toggle) { }
        public void UnregisterToggle(Toggle toggle) { }
        public void NotifyToggleOn(Toggle toggle, bool sendCallback = true) { }
        public bool AnyTogglesOn() => false;
        public System.Collections.Generic.IEnumerable<Toggle> ActiveToggles()
            => System.Array.Empty<Toggle>();
        public Toggle GetFirstActiveToggle() => null!;
        public void EnsureValidState() { }
        public void SetAllTogglesOff(bool sendCallback = true) { }
    }

    public partial class Slider : Selectable, ICanvasElement
    {
        public class SliderEvent : UnityEngine.Events.UnityEvent<float> { }
        public float minValue { get; set; }
        public float maxValue { get; set; } = 1;
        public bool wholeNumbers { get; set; }
        public float value { get; set; }
        public SliderEvent onValueChanged { get; set; } = new();
        public void SetValueWithoutNotify(float value) { }
        public float normalizedValue { get; set; }
        public UnityEngine.RectTransform fillRect { get; set; }
        public UnityEngine.RectTransform handleRect { get; set; }
        public Direction direction { get; set; }
        public void SetDirection(Direction direction, bool includeRectLayouts) { }
        public void Rebuild(CanvasUpdate executing) { }
        public void LayoutComplete() { }
        public void GraphicUpdateComplete() { }
        public void OnInitializePotentialDrag(UnityEngine.EventSystems.PointerEventData eventData) { }
        public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData) { }
        public override void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData) { }
        public override void OnMove(UnityEngine.EventSystems.AxisEventData eventData) { }
    }

    public partial class Scrollbar : Selectable, ICanvasElement
    {
        public class ScrollEvent : UnityEngine.Events.UnityEvent<float> { }
        public float value { get; set; }
        public float size { get; set; } = 0.2f;
        public int numberOfSteps { get; set; }
        public ScrollEvent onValueChanged { get; set; } = new();
        public void SetValueWithoutNotify(float value) { }
        public UnityEngine.RectTransform handleRect { get; set; }
        public Direction direction { get; set; }
        public void SetDirection(Direction direction, bool includeRectLayouts) { }
        public void Rebuild(CanvasUpdate executing) { }
        public void LayoutComplete() { }
        public void GraphicUpdateComplete() { }
        public void OnInitializePotentialDrag(UnityEngine.EventSystems.PointerEventData eventData) { }
        public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData) { }
        public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData) { }
        public override void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData) { }
        public override void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData) { }
        public override void OnMove(UnityEngine.EventSystems.AxisEventData eventData) { }
    }

    public enum CanvasUpdate { Prelayout, Layout, PostLayout, PreRender, LatePreRender, MaxUpdateValue }
    public class ScrollRect : UnityEngine.Behaviour, ICanvasElement, ILayoutElement, ILayoutGroup
    {
        public class ScrollRectEvent : UnityEngine.Events.UnityEvent<UnityEngine.Vector2> { }
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public enum ScrollbarVisibility { Permanent, AutoHide, AutoHideAndExpandViewport }
        public UnityEngine.RectTransform content { get; set; }
        public UnityEngine.RectTransform viewport { get; set; }
        public bool horizontal { get; set; } = true;
        public bool vertical { get; set; } = true;
        public MovementType movementType { get; set; } = MovementType.Elastic;
        public float elasticity { get; set; } = 0.1f;
        public bool inertia { get; set; } = true;
        public float decelerationRate { get; set; } = 0.135f;
        public float scrollSensitivity { get; set; } = 1;
        public UnityEngine.Vector2 velocity { get; set; }
        public Scrollbar horizontalScrollbar { get; set; }
        public Scrollbar verticalScrollbar { get; set; }
        public ScrollbarVisibility horizontalScrollbarVisibility { get; set; }
        public ScrollbarVisibility verticalScrollbarVisibility { get; set; }
        public float horizontalScrollbarSpacing { get; set; }
        public float verticalScrollbarSpacing { get; set; }
        public UnityEngine.Vector2 normalizedPosition { get; set; }
        public float horizontalNormalizedPosition { get; set; }
        public float verticalNormalizedPosition { get; set; }
        public ScrollRectEvent onValueChanged { get; set; } = new();
        public void StopMovement() { }
        public bool IsActive() => true;
        public bool IsDestroyed() => false;
        public float minWidth => -1;
        public float minHeight => -1;
        public float preferredWidth => -1;
        public float preferredHeight => -1;
        public float flexibleWidth => -1;
        public float flexibleHeight => -1;
        public int layoutPriority => -1;
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public void LayoutComplete() { }
        public void GraphicUpdateComplete() { }
        public void Rebuild(CanvasUpdate update) { }
        public void SetLayoutHorizontal() { }
        public void SetLayoutVertical() { }
        public void OnInitializePotentialDrag(UnityEngine.EventSystems.PointerEventData data) { }
        public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData data) { }
        public void OnDrag(UnityEngine.EventSystems.PointerEventData data) { }
        public void OnEndDrag(UnityEngine.EventSystems.PointerEventData data) { }
        public void OnScroll(UnityEngine.EventSystems.PointerEventData data) { }
    }

    public class InputField : Selectable, ICanvasElement, ILayoutElement
    {
        public class OnChangeEvent : UnityEngine.Events.UnityEvent<string> { }
        public class EndEditEvent : UnityEngine.Events.UnityEvent<string> { }
        public class SubmitEvent : UnityEngine.Events.UnityEvent<string> { }
        public enum ContentType { Standard, Autocorrected, IntegerNumber, DecimalNumber,
            Alphanumeric, Name, EmailAddress, Password, Pin, Custom }
        public enum InputType { Standard, AutoCorrect, Password }
        public enum CharacterValidation { None, Integer, Decimal, Alphanumeric, Name, EmailAddress }
        public enum LineType { SingleLine, MultiLineSubmit, MultiLineNewline }
        public delegate char OnValidateInput(string text, int charIndex, char addedChar);
        public string text { get; set; } = "";
        public int characterLimit { get; set; }
        public ContentType contentType { get; set; }
        public InputType inputType { get; set; }
        public CharacterValidation characterValidation { get; set; }
        public UnityEngine.TouchScreenKeyboardType keyboardType { get; set; }
        public LineType lineType { get; set; }
        public bool multiLine { get; }
        public char asteriskChar { get; set; } = '*';
        public float caretBlinkRate { get; set; }
        public int caretWidth { get; set; }
        public UnityEngine.Color caretColor { get; set; }
        public UnityEngine.Color selectionColor { get; set; }
        public bool customCaretColor { get; set; }
        public bool shouldActivateOnSelect { get; set; } = true;
        public bool shouldHideMobileInput { get; set; }
        public bool readOnly { get; set; }
        public bool isFocused { get; }
        public bool wasCanceled { get; }
        public int caretPosition { get; set; }
        public int selectionAnchorPosition { get; set; }
        public int selectionFocusPosition { get; set; }
        public int caretSelectPosition { get; }
        public UnityEngine.TouchScreenKeyboard touchScreenKeyboard { get; }
        public float minWidth => 5;
        public float minHeight => 0;
        public float preferredWidth => 0;
        public float preferredHeight => 0;
        public float flexibleWidth => -1;
        public float flexibleHeight => -1;
        public int layoutPriority => 1;
        public Text textComponent { get; set; }
        public Graphic placeholder { get; set; }
        public OnChangeEvent onValueChanged { get; set; } = new();
        public OnChangeEvent onValueChange { get; }
        public EndEditEvent onEndEdit { get; set; } = new();
        public SubmitEvent onSubmit { get; set; } = new();
        public OnValidateInput onValidateInput { get; set; }
        public void SetTextWithoutNotify(string value) { }
        public void ActivateInputField() { }
        public void DeactivateInputField() { }
        public void MoveTextStart(bool shift) { }
        public void MoveTextEnd(bool shift) { }
        public void ForceLabelUpdate() { }
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public void LayoutComplete() { }
        public void GraphicUpdateComplete() { }
        public void Rebuild(CanvasUpdate update) { }
        public UnityEngine.Vector2 ScreenToLocal(UnityEngine.Vector2 screen) => screen;
        public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData data) { }
        public void OnDrag(UnityEngine.EventSystems.PointerEventData data) { }
        public void OnEndDrag(UnityEngine.EventSystems.PointerEventData data) { }
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData data) { }
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData data) { }
        public void OnSelect(UnityEngine.EventSystems.BaseEventData data) { }
        public void OnDeselect(UnityEngine.EventSystems.BaseEventData data) { }
        public void OnSubmit(UnityEngine.EventSystems.BaseEventData data) { }
        public void OnUpdateSelected(UnityEngine.EventSystems.BaseEventData data) { }
        public void ProcessEvent(UnityEngine.Event data) { }
    }

    public class LayoutElement : UnityEngine.Behaviour, ILayoutElement, ILayoutIgnorer
    {
        public bool ignoreLayout { get; set; }
        public float minWidth { get; set; } = -1;
        public float minHeight { get; set; } = -1;
        public float preferredWidth { get; set; } = -1;
        public float preferredHeight { get; set; } = -1;
        public float flexibleWidth { get; set; } = -1;
        public float flexibleHeight { get; set; } = -1;
        public int layoutPriority { get; set; } = 1;
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
    }
    public static class LayoutUtility
    {
        public static float GetLayoutProperty(UnityEngine.RectTransform rect,
            System.Func<ILayoutElement, float> property, float defaultValue) => defaultValue;
        public static float GetLayoutProperty(UnityEngine.RectTransform rect,
            System.Func<ILayoutElement, float> property, float defaultValue,
            out ILayoutElement source) { source = null; return defaultValue; }
        public static float GetMinWidth(UnityEngine.RectTransform rect) => 0;
        public static float GetMinHeight(UnityEngine.RectTransform rect) => 0;
        public static float GetPreferredWidth(UnityEngine.RectTransform rect) => 0;
        public static float GetPreferredHeight(UnityEngine.RectTransform rect) => 0;
        public static float GetFlexibleWidth(UnityEngine.RectTransform rect) => 0;
        public static float GetFlexibleHeight(UnityEngine.RectTransform rect) => 0;
        public static float GetMinSize(UnityEngine.RectTransform rect, int axis) => 0;
        public static float GetPreferredSize(UnityEngine.RectTransform rect, int axis) => 0;
        public static float GetFlexibleSize(UnityEngine.RectTransform rect, int axis) => 0;
    }
    public class LayoutRebuilder : ICanvasElement
    {
        public UnityEngine.RectTransform transform { get; }
        UnityEngine.Transform ICanvasElement.transform => transform;
        public static void ForceRebuildLayoutImmediate(UnityEngine.RectTransform rect) { }
        public static void MarkLayoutForRebuild(UnityEngine.RectTransform rect) { }
        public void Rebuild(CanvasUpdate update) { }
        public void LayoutComplete() { }
        public void GraphicUpdateComplete() { }
        public bool IsDestroyed() => false;
        public override bool Equals(object obj) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
    }

    public class ContentSizeFitter : UnityEngine.Behaviour, ILayoutSelfController
    {
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
        public FitMode horizontalFit { get; set; }
        public FitMode verticalFit { get; set; }
        public void SetLayoutHorizontal() { }
        public void SetLayoutVertical() { }
    }
    public class AspectRatioFitter : UnityEngine.EventSystems.UIBehaviour, ILayoutSelfController
    {
        public enum AspectMode { None, WidthControlsHeight, HeightControlsWidth,
            FitInParent, EnvelopeParent }
        public AspectMode aspectMode { get; set; }
        public float aspectRatio { get; set; } = 1;
        public bool IsComponentValidOnObject() => true;
        public bool IsAspectModeValid() => true;
        public void SetLayoutHorizontal() { }
        public void SetLayoutVertical() { }
    }

    public class LayoutGroup : UnityEngine.Behaviour, ILayoutElement, ILayoutGroup
    {
        public UnityEngine.RectOffset padding { get; set; } = new();
        public UnityEngine.TextAnchor childAlignment { get; set; }
        public virtual float minWidth => 0;
        public virtual float minHeight => 0;
        public virtual float preferredWidth => 0;
        public virtual float preferredHeight => 0;
        public virtual float flexibleWidth => 0;
        public virtual float flexibleHeight => 0;
        public virtual int layoutPriority => 0;
        public virtual void CalculateLayoutInputHorizontal() { }
        public virtual void CalculateLayoutInputVertical() { }
        public virtual void SetLayoutHorizontal() { }
        public virtual void SetLayoutVertical() { }
    }

    public partial class GridLayoutGroup : LayoutGroup
    {
        public enum Corner { UpperLeft, UpperRight, LowerLeft, LowerRight }
        public enum Axis { Horizontal, Vertical }
        public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }
        public Corner startCorner { get; set; }
        public Axis startAxis { get; set; }
        public UnityEngine.Vector2 cellSize { get; set; } = new(100, 100);
        public UnityEngine.Vector2 spacing { get; set; }
        public Constraint constraint { get; set; }
        public int constraintCount { get; set; } = 2;
    }

    public class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childForceExpandWidth { get; set; } = true;
        public bool childForceExpandHeight { get; set; } = true;
        public bool childControlWidth { get; set; } = true;
        public bool childControlHeight { get; set; } = true;
        public bool childScaleWidth { get; set; }
        public bool childScaleHeight { get; set; }
        public bool reverseArrangement { get; set; }
    }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }

    public abstract class Graphic : UnityEngine.Behaviour, ICanvasElement
    {
        public static UnityEngine.Material defaultGraphicMaterial { get; }
        public virtual UnityEngine.Material defaultMaterial { get; }
        public int depth { get; }
        public UnityEngine.Color color { get; set; }
        public bool raycastTarget { get; set; } = true;
        public UnityEngine.RectTransform rectTransform { get; }
        public UnityEngine.Canvas canvas { get; }
        public UnityEngine.CanvasRenderer canvasRenderer { get; }
        public UnityEngine.Texture mainTexture { get; }
        public UnityEngine.Vector4 raycastPadding { get; set; }
        public UnityEngine.Rect GetPixelAdjustedRect() => default;
        public UnityEngine.Vector2 PixelAdjustPoint(UnityEngine.Vector2 point) => point;
        public virtual bool Raycast(UnityEngine.Vector2 point,
            UnityEngine.Camera camera) => true;
        public virtual void SetNativeSize() { }
        public void SetAllDirty() { }
        public void SetLayoutDirty() { }
        public void SetVerticesDirty() { }
        public void SetMaterialDirty() { }
        public void SetRaycastDirty() { }
        public void RegisterDirtyLayoutCallback(UnityEngine.Events.UnityAction action) { }
        public void UnregisterDirtyLayoutCallback(UnityEngine.Events.UnityAction action) { }
        public void RegisterDirtyVerticesCallback(UnityEngine.Events.UnityAction action) { }
        public void UnregisterDirtyVerticesCallback(UnityEngine.Events.UnityAction action) { }
        public void RegisterDirtyMaterialCallback(UnityEngine.Events.UnityAction action) { }
        public void UnregisterDirtyMaterialCallback(UnityEngine.Events.UnityAction action) { }
        public void LayoutComplete() { }
        public void GraphicUpdateComplete() { }
        public bool IsDestroyed() => false;
        public void OnCullingChanged() { }
        public void OnRebuildRequested() { }
        public void Rebuild(CanvasUpdate update) { }
        public void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale) { }
        public void CrossFadeColor(UnityEngine.Color targetColor, float duration,
            bool ignoreTimeScale, bool useAlpha) { }
        public void CrossFadeColor(UnityEngine.Color targetColor, float duration,
            bool ignoreTimeScale, bool useAlpha, bool useRGB) { }
    }

    public abstract class MaskableGraphic : Graphic, IClippable, IMaskable
    {
        public class CullStateChangedEvent : UnityEngine.Events.UnityEvent<bool> { }
        public bool maskable { get; set; } = true;
        public bool isMaskingGraphic { get; }
        public CullStateChangedEvent onCullStateChanged { get; }
        public void Cull(UnityEngine.Rect clipRect, bool validRect) { }
        public void ParentMaskStateChanged() { }
        public void RecalculateClipping() { }
        public void RecalculateMasking() { }
        public UnityEngine.Material GetModifiedMaterial(UnityEngine.Material baseMaterial)
            => baseMaterial;
        public void SetClipRect(UnityEngine.Rect clipRect, bool validRect) { }
        public void SetClipSoftness(UnityEngine.Vector2 softness) { }
    }

    public partial class Image : MaskableGraphic, ILayoutElement
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
        public UnityEngine.Sprite sprite { get; set; }
        public UnityEngine.Sprite overrideSprite { get; set; }
        public UnityEngine.Texture mainTexture { get; }
        public static UnityEngine.Material defaultETC1GraphicMaterial { get; }
        public UnityEngine.Material material { get; set; }
        public bool useSpriteMesh { get; set; }
        public bool hasBorder { get; }
        public float pixelsPerUnit { get; }
        public float minWidth { get; }
        public float minHeight { get; }
        public float preferredWidth { get; }
        public float preferredHeight { get; }
        public float flexibleWidth { get; }
        public float flexibleHeight { get; }
        public int layoutPriority { get; }
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public void SetNativeSize() { }
        public Type type { get; set; }
        public bool preserveAspect { get; set; }
        public bool fillCenter { get; set; } = true;
        public float pixelsPerUnitMultiplier { get; set; } = 1;
        public FillMethod fillMethod { get; set; } = FillMethod.Radial360;
        public int fillOrigin { get; set; }
        public bool fillClockwise { get; set; } = true;
        public float fillAmount { get; set; } = 1;
        public float alphaHitTestMinimumThreshold { get; set; }
        public float eventAlphaThreshold { get; set; } = 1;
        public bool IsRaycastLocationValid(UnityEngine.Vector2 screenPoint,
            UnityEngine.Camera eventCamera) => true;
        public void DisableSpriteOptimizations() { }
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { }
    }

    public class RawImage : MaskableGraphic
    {
        public UnityEngine.Texture texture { get; set; }
        public UnityEngine.Texture mainTexture { get; }
        public UnityEngine.Rect uvRect { get; set; }
        public void SetNativeSize() { }
    }

    public class FontData
    {
        public static FontData defaultFontData => new();
        public UnityEngine.Font font { get; set; }
        public int fontSize { get; set; }
        public UnityEngine.FontStyle fontStyle { get; set; }
        public bool bestFit { get; set; }
        public int minSize { get; set; }
        public int maxSize { get; set; }
        public UnityEngine.TextAnchor alignment { get; set; }
        public bool alignByGeometry { get; set; }
        public bool richText { get; set; }
        public UnityEngine.HorizontalWrapMode horizontalOverflow { get; set; }
        public UnityEngine.VerticalWrapMode verticalOverflow { get; set; }
        public float lineSpacing { get; set; }
    }
    public class Text : MaskableGraphic, ILayoutElement
    {
        public string text { get; set; }
        public UnityEngine.Font font { get; set; }
        public UnityEngine.Texture mainTexture { get; }
        public object cachedTextGenerator { get; }
        public object cachedTextGeneratorForLayout { get; }
        public object GetGenerationSettings(UnityEngine.Vector2 extents) => null!;
        public int fontSize { get; set; }
        public UnityEngine.FontStyle fontStyle { get; set; }
        public UnityEngine.TextAnchor alignment { get; set; }
        public bool alignByGeometry { get; set; }
        public bool supportRichText { get; set; }
        public UnityEngine.HorizontalWrapMode horizontalOverflow { get; set; }
        public UnityEngine.VerticalWrapMode verticalOverflow { get; set; }
        public float lineSpacing { get; set; }
        public bool resizeTextForBestFit { get; set; }
        public int resizeTextMinSize { get; set; }
        public int resizeTextMaxSize { get; set; }
        public float pixelsPerUnit { get; }
        public float minWidth { get; }
        public float minHeight { get; }
        public float preferredWidth { get; }
        public float preferredHeight { get; }
        public float flexibleWidth { get; }
        public float flexibleHeight { get; }
        public int layoutPriority { get; }
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public void FontTextureChanged() { }
        public void OnRebuildRequested() { }
    }
    public class Shadow : UnityEngine.Behaviour
    {
        public UnityEngine.Color effectColor { get; set; }
        public UnityEngine.Vector2 effectDistance { get; set; }
        public bool useGraphicAlpha { get; set; }
        public void ModifyMesh(object mesh) { }
    }
    public class Outline : Shadow { }
}

namespace UnityEngine.EventSystems
{
    public abstract class UIBehaviour : UnityEngine.MonoBehaviour
    {
        public virtual bool IsActive() => isActiveAndEnabled;
        public bool IsDestroyed() => false;
    }
    public class BaseRaycaster : UIBehaviour
    {
        public virtual UnityEngine.Camera eventCamera => null;
        public virtual int priority => 0;
        public virtual int renderOrderPriority => 0;
        public virtual int sortOrderPriority => 0;
        public BaseRaycaster rootRaycaster => this;
        public virtual void Raycast(PointerEventData eventData,
            System.Collections.Generic.List<RaycastResult> resultAppendList) { }
    }
    public class PhysicsRaycaster : BaseRaycaster
    {
        public int depth { get; }
        public override UnityEngine.Camera eventCamera => null;
        public UnityEngine.LayerMask eventMask { get; set; }
        public int finalEventMask { get; }
        public int maxRayIntersections { get; set; }
    }
    public class Physics2DRaycaster : PhysicsRaycaster { }
    public static class RaycasterManager
    {
        public static System.Collections.Generic.List<BaseRaycaster> GetRaycasters() => new();
    }
    public struct RaycastResult
    {
        public GameObject gameObject { get; set; }
        public BaseRaycaster module { get; set; }
        public UnityEngine.Vector2 screenPosition { get; set; }
        public UnityEngine.Vector3 worldPosition { get; set; }
        public UnityEngine.Vector3 worldNormal { get; set; }
        public int sortingOrder { get; set; }
        public int sortingLayer { get; set; }
        public int sortingGroupID { get; set; }
        public int sortingGroupOrder { get; set; }
        public int depth { get; set; }
        public float index { get; set; }
        public float distance { get; set; }
        public int displayIndex { get; set; }
        public UnityEngine.Vector3 origin { get; set; }
        public UnityEngine.UIElements.UIDocument document { get; set; }
        public UnityEngine.UIElements.VisualElement element { get; set; }
        public bool isValid => gameObject != null && module != null;
        public void Clear() { }
    }
    public abstract class AbstractEventData
    {
        public virtual bool used { get; }
        public virtual void Use() { }
        public virtual void Reset() { }
    }
    public class BaseEventData : AbstractEventData
    {
        public BaseEventData(EventSystem eventSystem) { }
        public BaseInputModule currentInputModule { get; }
        public GameObject selectedObject { get; set; }
    }

    public partial class PointerEventData : BaseEventData
    {
        public enum InputButton { Left = 0, Right = 1, Middle = 2 }
        public PointerEventData(EventSystem eventSystem) : base(eventSystem) { }
        public int pointerId { get; set; }
        public UnityEngine.Vector2 position { get; set; }
        public UnityEngine.Vector2 delta { get; set; }
        public UnityEngine.Vector2 pressPosition { get; set; }
        public UnityEngine.Vector2 scrollDelta { get; set; }
        public InputButton button { get; set; }
        public int clickCount { get; set; }
        public GameObject pointerPress { get; set; }
        public GameObject pointerEnter { get; set; }
        public GameObject pointerDrag { get; set; }
        public GameObject pointerClick { get; set; }
        public GameObject rawPointerPress { get; set; }
        public GameObject lastPress { get; set; }
        public System.Collections.Generic.List<GameObject> hovered { get; } = new();
        public RaycastResult pointerCurrentRaycast { get; set; }
        public RaycastResult pointerPressRaycast { get; set; }
        public UnityEngine.Camera enterEventCamera { get; }
        public UnityEngine.Camera pressEventCamera { get; }
        public UnityEngine.Vector3 worldPosition { get; set; }
        public UnityEngine.Vector3 worldNormal { get; set; }
        public float clickTime { get; set; }
        public int displayIndex { get; set; }
        public float pressure { get; set; }
        public float tangentialPressure { get; set; }
        public float altitudeAngle { get; set; }
        public float azimuthAngle { get; set; }
        public UnityEngine.Vector2 radius { get; set; }
        public UnityEngine.Vector2 radiusVariance { get; set; }
        public UnityEngine.Vector2 tilt { get; set; }
        public float twist { get; set; }
        public UnityEngine.PenStatus penStatus { get; set; }
        public bool fullyExited { get; set; }
        public bool reentered { get; set; }
        public bool eligibleForClick { get; set; }
        public bool useDragThreshold { get; set; }
        public bool dragging { get; set; }
        public bool IsPointerMoving() => false;
        public bool IsScrolling() => false;
    }

    public interface IEventSystemHandler { }
    public interface IPointerMoveHandler : IEventSystemHandler { void OnPointerMove(PointerEventData eventData); }
    public interface IPointerEnterHandler : IEventSystemHandler { void OnPointerEnter(PointerEventData eventData); }
    public interface IPointerExitHandler : IEventSystemHandler { void OnPointerExit(PointerEventData eventData); }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData eventData); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData eventData); }
    public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData eventData); }
    public interface IInitializePotentialDragHandler : IEventSystemHandler { void OnInitializePotentialDrag(PointerEventData eventData); }
    public interface IBeginDragHandler : IEventSystemHandler { void OnBeginDrag(PointerEventData eventData); }
    public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData eventData); }
    public interface IEndDragHandler : IEventSystemHandler { void OnEndDrag(PointerEventData eventData); }
    public interface IDropHandler : IEventSystemHandler { void OnDrop(PointerEventData eventData); }
    public interface IScrollHandler : IEventSystemHandler { void OnScroll(PointerEventData eventData); }
    public interface IUpdateSelectedHandler : IEventSystemHandler { void OnUpdateSelected(BaseEventData eventData); }
    public interface ISelectHandler : IEventSystemHandler { void OnSelect(BaseEventData eventData); }
    public interface IDeselectHandler : IEventSystemHandler { void OnDeselect(BaseEventData eventData); }
    public interface ISubmitHandler : IEventSystemHandler { void OnSubmit(BaseEventData eventData); }
    public interface ICancelHandler : IEventSystemHandler { void OnCancel(BaseEventData eventData); }
    public interface IMoveHandler : IEventSystemHandler { void OnMove(AxisEventData eventData); }
    public enum MoveDirection { Left = 0, Up = 1, Right = 2, Down = 3, None = 4 }
    public class AxisEventData : BaseEventData
    {
        public AxisEventData(EventSystem eventSystem) : base(eventSystem) { }
        public UnityEngine.Vector2 moveVector { get; set; }
        public MoveDirection moveDir { get; set; }
    }

    public static class ExecuteEvents
    {
        public delegate void EventFunction<T1>(T1 handler, BaseEventData eventData);
        public static T ValidateEventData<T>(BaseEventData data) where T : class => null!;
        public static bool Execute<T>(GameObject target, BaseEventData eventData,
            EventFunction<T> functor) where T : IEventSystemHandler => false;
        public static GameObject ExecuteHierarchy<T>(GameObject root, BaseEventData eventData,
            EventFunction<T> functor) where T : IEventSystemHandler => null!;
        public static bool CanHandleEvent<T>(GameObject target) where T : IEventSystemHandler => false;
        public static GameObject GetEventHandler<T>(GameObject root) where T : IEventSystemHandler => null!;
        public static EventFunction<IPointerMoveHandler> pointerMoveHandler { get; }
        public static EventFunction<IPointerEnterHandler> pointerEnterHandler { get; }
        public static EventFunction<IPointerExitHandler> pointerExitHandler { get; }
        public static EventFunction<IPointerDownHandler> pointerDownHandler { get; }
        public static EventFunction<IPointerUpHandler> pointerUpHandler { get; }
        public static EventFunction<IPointerClickHandler> pointerClickHandler { get; }
        public static EventFunction<IInitializePotentialDragHandler> initializePotentialDrag { get; }
        public static EventFunction<IBeginDragHandler> beginDragHandler { get; }
        public static EventFunction<IDragHandler> dragHandler { get; }
        public static EventFunction<IEndDragHandler> endDragHandler { get; }
        public static EventFunction<IDropHandler> dropHandler { get; }
        public static EventFunction<IScrollHandler> scrollHandler { get; }
        public static EventFunction<IUpdateSelectedHandler> updateSelectedHandler { get; }
        public static EventFunction<ISelectHandler> selectHandler { get; }
        public static EventFunction<IDeselectHandler> deselectHandler { get; }
        public static EventFunction<IMoveHandler> moveHandler { get; }
        public static EventFunction<ISubmitHandler> submitHandler { get; }
        public static EventFunction<ICancelHandler> cancelHandler { get; }
    }

    public class EventTrigger : UnityEngine.Behaviour, IPointerEnterHandler,
        IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler, IInitializePotentialDragHandler, IBeginDragHandler,
        IDragHandler, IEndDragHandler, IDropHandler, IScrollHandler,
        IUpdateSelectedHandler, ISelectHandler, IDeselectHandler, IMoveHandler,
        ISubmitHandler, ICancelHandler
    {
        public class TriggerEvent : UnityEngine.Events.UnityEvent<BaseEventData> { }
        public class Entry
        {
            public EventTriggerType eventID { get; set; } = EventTriggerType.PointerClick;
            public TriggerEvent callback { get; set; } = new();
        }
        public System.Collections.Generic.List<Entry> triggers { get; set; } = new();
        public System.Collections.Generic.List<Entry> delegates { get; set; } = new();
        public virtual void OnPointerEnter(PointerEventData data) { }
        public virtual void OnPointerExit(PointerEventData data) { }
        public virtual void OnPointerDown(PointerEventData data) { }
        public virtual void OnPointerUp(PointerEventData data) { }
        public virtual void OnPointerClick(PointerEventData data) { }
        public virtual void OnInitializePotentialDrag(PointerEventData data) { }
        public virtual void OnBeginDrag(PointerEventData data) { }
        public virtual void OnDrag(PointerEventData data) { }
        public virtual void OnEndDrag(PointerEventData data) { }
        public virtual void OnDrop(PointerEventData data) { }
        public virtual void OnScroll(PointerEventData data) { }
        public virtual void OnUpdateSelected(BaseEventData data) { }
        public virtual void OnSelect(BaseEventData data) { }
        public virtual void OnDeselect(BaseEventData data) { }
        public virtual void OnMove(AxisEventData data) { }
        public virtual void OnSubmit(BaseEventData data) { }
        public virtual void OnCancel(BaseEventData data) { }
    }

    public class EventSystem : UIBehaviour
    {
        public static void SetUITookitEventSystemOverride(EventSystem activeEventSystem,
            bool sendEvents = true, bool createPanelGameObjectsOnStart = true) { }
        public static EventSystem current { get; set; }
        public GameObject firstSelectedGameObject { get; set; }
        public GameObject currentSelectedGameObject { get; }
        public GameObject lastSelectedGameObject { get; }
        public BaseInputModule currentInputModule { get; }
        public bool alreadySelecting { get; }
        public bool isFocused { get; }
        public bool sendNavigationEvents { get; set; } = true;
        public int pixelDragThreshold { get; set; } = 10;
        public void UpdateModules() { }
        public void SetSelectedGameObject(GameObject selected) { }
        public void SetSelectedGameObject(GameObject selected, BaseEventData pointer) { }
        public bool IsPointerOverGameObject() => false;
        public bool IsPointerOverGameObject(int pointerId) => false;
        public void RaycastAll(PointerEventData eventData,
            System.Collections.Generic.List<RaycastResult> raycastResults) { }
    }

    public class BaseInput : UnityEngine.Behaviour
    {
        public virtual string compositionString { get; }
        public virtual UnityEngine.IMECompositionMode imeCompositionMode { get; set; }
        public virtual UnityEngine.Vector2 compositionCursorPos { get; set; }
        public virtual bool mousePresent { get; }
        public virtual bool GetMouseButtonDown(int button) => false;
        public virtual bool GetMouseButtonUp(int button) => false;
        public virtual bool GetMouseButton(int button) => false;
        public virtual UnityEngine.Vector2 mousePosition { get; }
        public virtual UnityEngine.Vector2 mouseScrollDelta { get; }
        public virtual float mouseScrollDeltaPerTick => 1f;
        public virtual bool touchSupported { get; }
        public virtual int touchCount { get; }
        public virtual UnityEngine.Touch GetTouch(int index) => default;
        public virtual float GetAxisRaw(string axisName) => 0;
        public virtual bool GetButtonDown(string buttonName) => false;
    }

    public abstract class BaseInputModule : UIBehaviour
    {
        public BaseInput input { get; }
        public BaseInput inputOverride { get; set; }
        public virtual void Process() { }
        public virtual void ActivateModule() { }
        public virtual void DeactivateModule() { }
        public virtual void UpdateModule() { }
        public virtual bool IsModuleSupported() => true;
        public virtual bool ShouldActivateModule() => true;
        public virtual bool IsPointerOverGameObject(int pointerId) => false;
        public virtual UnityEngine.Vector2 ConvertPointerEventScrollDeltaToTicks(UnityEngine.Vector2 scrollDelta) => scrollDelta;
        public virtual int ConvertUIToolkitPointerId(PointerEventData data) => 0;
        public virtual NavigationDeviceType GetNavigationEventDeviceType(BaseEventData data) => NavigationDeviceType.Unknown;
    }

    public abstract class PointerInputModule : BaseInputModule
    {
        public class MouseButtonEventData
        {
            public PointerEventData.FramePressState buttonState { get; set; }
            public PointerEventData buttonData { get; set; }
            public bool PressedThisFrame() => false;
            public bool ReleasedThisFrame() => false;
        }
        public const int kMouseLeftId = -1;
        public const int kMouseRightId = -2;
        public const int kMouseMiddleId = -3;
        public const int kFakeTouchesId = -4;
        public virtual bool IsPointerOverGameObject(int pointerId) => false;
    }

    public partial class StandaloneInputModule : PointerInputModule
    {
        public InputMode inputMode { get; }
        public bool forceModuleActive { get; set; }
        public bool allowActivationOnMobileDevice { get; set; }
        public string horizontalAxis { get; set; } = "Horizontal";
        public string verticalAxis { get; set; } = "Vertical";
        public string submitButton { get; set; } = "Submit";
        public string cancelButton { get; set; } = "Cancel";
        public float inputActionsPerSecond { get; set; } = 10;
        public float repeatDelay { get; set; } = 0.5f;
    }
    public class TouchInputModule : PointerInputModule
    {
        public bool allowActivationOnStandalone { get; set; }
        public bool forceModuleActive { get; set; }
    }
}

namespace UnityEngine.InputSystem.UI
{
    public enum UIPointerBehavior
    {
        SingleMouseOrPenButMultiTouchAndTrack = 0,
        SingleUnifiedPointer = 1,
        AllPointersAsIs = 2
    }

    public class InputSystemUIInputModule : UnityEngine.EventSystems.BaseInputModule
    {
        public UnityEngine.InputSystem.InputActionAsset actionsAsset { get; set; }
        public UnityEngine.InputSystem.InputActionReference point { get; set; }
        public UnityEngine.InputSystem.InputActionReference scrollWheel { get; set; }
        public UnityEngine.InputSystem.InputActionReference leftClick { get; set; }
        public UnityEngine.InputSystem.InputActionReference middleClick { get; set; }
        public UnityEngine.InputSystem.InputActionReference rightClick { get; set; }
        public UnityEngine.InputSystem.InputActionReference move { get; set; }
        public UnityEngine.InputSystem.InputActionReference submit { get; set; }
        public UnityEngine.InputSystem.InputActionReference cancel { get; set; }
        public UnityEngine.InputSystem.InputActionReference trackedDevicePosition { get; set; }
        public UnityEngine.InputSystem.InputActionReference trackedDeviceOrientation { get; set; }
        public bool deselectOnBackgroundClick { get; set; } = true;
        public UIPointerBehavior pointerBehavior { get; set; }
        public float scrollDeltaPerTick { get; set; } = 6;
        public float moveRepeatDelay { get; set; } = 0.5f;
        public float moveRepeatRate { get; set; } = 0.1f;
    }
}

namespace UnityEngine.UIElements
{
    public sealed class VectorImage { }
    public enum StyleKeyword { Undefined, Null, Auto, None, Initial }
    public interface IStyleValue<T>
    {
        T value { get; }
        StyleKeyword keyword { get; }
    }
    public enum LengthUnit { Pixel = 0, Percent = 1 }
    public readonly struct Length
    {
        public float value { get; }
        public LengthUnit unit { get; }
        public Length(float value, LengthUnit unit = LengthUnit.Pixel)
        { this.value = value; this.unit = unit; }
        public static Length Auto() => new();
        public static Length None() => new();
        public static Length Pixels(float value) => new(value);
        public static Length Percent(float value) => new(value, LengthUnit.Percent);
        public bool IsAuto() => false;
        public bool IsNone() => false;
        public bool Equals(Length other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(Length left, Length right) => false;
        public static bool operator !=(Length left, Length right) => false;
        public static implicit operator Length(float value) => new(value);
    }
    public readonly struct Angle
    {
        public float value { get; }
        public AngleUnit unit { get; }
        public Angle(float value) { this.value = value; }
        public Angle(float value, AngleUnit unit) { this.value = value; this.unit = unit; }
        public static Angle Degrees(float value) => new(value);
        public static Angle Gradians(float value) => new(value, AngleUnit.Gradian);
        public static Angle Radians(float value) => new(value, AngleUnit.Radian);
        public static Angle Turns(float value) => new(value, AngleUnit.Turn);
        public float ToDegrees() => value;
        public float ToGradians() => value;
        public float ToRadians() => value;
        public float ToTurns() => value;
        public bool Equals(Angle other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static implicit operator Angle(float value) => new(value);
        public static bool operator ==(Angle left, Angle right) => false;
        public static bool operator !=(Angle left, Angle right) => false;
    }
    public readonly struct TimeValue
    {
        public float value { get; }
        public TimeUnit unit { get; }
        public TimeValue(float value) { this.value = value; }
        public TimeValue(float value, TimeUnit unit) { this.value = value; this.unit = unit; }
        public static TimeValue Seconds(float value) => new(value);
        public static TimeValue Milliseconds(float value) => new(value, TimeUnit.Millisecond);
        public bool Equals(TimeValue other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static implicit operator TimeValue(float value) => new(value);
        public static bool operator ==(TimeValue left, TimeValue right) => false;
        public static bool operator !=(TimeValue left, TimeValue right) => false;
    }
    public readonly struct StylePropertyName
    {
        public StylePropertyName(string name) { }
        public static bool IsNullOrEmpty(StylePropertyName value) => false;
        public bool Equals(StylePropertyName other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static implicit operator StylePropertyName(string value) => new(value);
        public static bool operator ==(StylePropertyName left, StylePropertyName right) => false;
        public static bool operator !=(StylePropertyName left, StylePropertyName right) => false;
    }
    public readonly struct EasingFunction
    {
        public EasingMode mode { get; }
        public EasingFunction(EasingMode mode) { this.mode = mode; }
        public bool Equals(EasingFunction other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static implicit operator EasingFunction(EasingMode value) => new(value);
        public static bool operator ==(EasingFunction left, EasingFunction right) => false;
        public static bool operator !=(EasingFunction left, EasingFunction right) => false;
    }
    public readonly struct StyleList<T> : IStyleValue<System.Collections.Generic.List<T>>
    {
        public System.Collections.Generic.List<T> value { get; }
        public StyleKeyword keyword { get; }
        public StyleList(System.Collections.Generic.List<T> value) { this.value = value; }
        public StyleList(StyleKeyword keyword) { this.keyword = keyword; }
        public bool Equals(StyleList<T> other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static implicit operator StyleList<T>(System.Collections.Generic.List<T> value) => new(value);
        public static implicit operator StyleList<T>(StyleKeyword value) => new(value);
        public static bool operator ==(StyleList<T> left, StyleList<T> right) => false;
        public static bool operator !=(StyleList<T> left, StyleList<T> right) => false;
    }
    public readonly struct StylePropertyNameCollection
    {
        public bool Contains(StylePropertyName name) => false;
        public Enumerator GetEnumerator() => default;
        public struct Enumerator : System.Collections.Generic.IEnumerator<StylePropertyName>
        {
            public StylePropertyName Current => default;
            object System.Collections.IEnumerator.Current => Current;
            public bool MoveNext() => false;
            public void Reset() { }
            public void Dispose() { }
        }
    }
    public struct Cursor
    {
        public UnityEngine.Texture2D texture;
        public UnityEngine.Vector2 hotspot;
        public bool Equals(Cursor other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(Cursor left, Cursor right) => false;
        public static bool operator !=(Cursor left, Cursor right) => false;
    }
    public readonly struct StyleCursor : IStyleValue<Cursor>
    {
        public Cursor value { get; }
        public StyleKeyword keyword { get; }
        public StyleCursor(Cursor value) { this.value = value; }
        public StyleCursor(StyleKeyword keyword) { this.keyword = keyword; }
        public static implicit operator StyleCursor(Cursor value) => new(value);
        public static implicit operator StyleCursor(StyleKeyword value) => new(value);
        public bool Equals(StyleCursor other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleCursor left, StyleCursor right) => false;
        public static bool operator !=(StyleCursor left, StyleCursor right) => false;
    }
    public struct FontDefinition
    {
        public UnityEngine.Font font;
        public object fontAsset;
        public bool Equals(FontDefinition other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(FontDefinition left, FontDefinition right) => false;
        public static bool operator !=(FontDefinition left, FontDefinition right) => false;
    }
    public readonly struct StyleFontDefinition : IStyleValue<FontDefinition>
    {
        public FontDefinition value { get; }
        public StyleKeyword keyword { get; }
        public StyleFontDefinition(FontDefinition value) { this.value = value; }
        public StyleFontDefinition(StyleKeyword keyword) { this.keyword = keyword; }
        public static implicit operator StyleFontDefinition(FontDefinition value) => new(value);
        public static implicit operator StyleFontDefinition(StyleKeyword value) => new(value);
        public bool Equals(StyleFontDefinition other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(StyleFontDefinition left, StyleFontDefinition right) => false;
        public static bool operator !=(StyleFontDefinition left, StyleFontDefinition right) => false;
    }
    public readonly struct StyleFont : IStyleValue<UnityEngine.Font>
    {
        public UnityEngine.Font value { get; }
        public StyleKeyword keyword { get; }
        public StyleFont(UnityEngine.Font value) { this.value = value; }
        public StyleFont(StyleKeyword keyword) { this.keyword = keyword; }
        public static implicit operator StyleFont(UnityEngine.Font value) => new(value);
        public static implicit operator StyleFont(StyleKeyword value) => new(value);
        public bool Equals(StyleFont other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleFont left, StyleFont right) => false;
        public static bool operator !=(StyleFont left, StyleFont right) => false;
    }
    public readonly struct TextAutoSize
    {
        public TextAutoSizeMode mode { get; }
        public Length minSize { get; }
        public Length maxSize { get; }
        public TextAutoSize(TextAutoSizeMode mode, Length minSize, Length maxSize)
        { this.mode = mode; this.minSize = minSize; this.maxSize = maxSize; }
        public static TextAutoSize None() => default;
        public bool Equals(TextAutoSize other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(TextAutoSize left, TextAutoSize right) => false;
        public static bool operator !=(TextAutoSize left, TextAutoSize right) => false;
    }
    public readonly struct StyleTextAutoSize : IStyleValue<TextAutoSize>
    {
        public TextAutoSize value { get; }
        public StyleKeyword keyword { get; }
        public StyleTextAutoSize(TextAutoSize value) { this.value = value; }
        public StyleTextAutoSize(StyleKeyword keyword) { this.keyword = keyword; }
        public static implicit operator StyleTextAutoSize(TextAutoSize value) => new(value);
        public static implicit operator StyleTextAutoSize(StyleKeyword value) => new(value);
        public bool Equals(StyleTextAutoSize other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleTextAutoSize left, StyleTextAutoSize right) => false;
        public static bool operator !=(StyleTextAutoSize left, StyleTextAutoSize right) => false;
    }
    public struct TextShadow
    {
        public UnityEngine.Vector2 offset;
        public float blurRadius;
        public UnityEngine.Color color;
        public bool Equals(TextShadow other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(TextShadow left, TextShadow right) => false;
        public static bool operator !=(TextShadow left, TextShadow right) => false;
    }
    public readonly struct TransformOrigin
    {
        public Length x { get; }
        public Length y { get; }
        public float z { get; }
        public TransformOrigin(Length x, Length y) { this.x = x; this.y = y; }
        public TransformOrigin(Length x, Length y, float z) { this.x = x; this.y = y; this.z = z; }
        public static TransformOrigin Initial() => default;
        public bool Equals(TransformOrigin other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(TransformOrigin left, TransformOrigin right) => false;
        public static bool operator !=(TransformOrigin left, TransformOrigin right) => false;
    }
    public readonly struct Translate
    {
        public Length x { get; }
        public Length y { get; }
        public float z { get; }
        public Translate(Length x, Length y) { this.x = x; this.y = y; }
        public Translate(Length x, Length y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Translate None() => default;
        public bool Equals(Translate other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static implicit operator Translate(UnityEngine.Vector2 value) => default;
        public static implicit operator Translate(UnityEngine.Vector3 value) => default;
        public static bool operator ==(Translate left, Translate right) => false;
        public static bool operator !=(Translate left, Translate right) => false;
    }
    public readonly struct Rotate
    {
        public Angle angle { get; }
        public Rotate(Angle angle) { this.angle = angle; }
        public Rotate(Angle angle, UnityEngine.Vector3 axis) { this.angle = angle; }
        public Rotate(UnityEngine.Quaternion value) { }
        public static Rotate None() => default;
        public bool Equals(Rotate other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static implicit operator Rotate(Angle value) => new(value);
        public static implicit operator Rotate(UnityEngine.Quaternion value) => new(value);
        public static bool operator ==(Rotate left, Rotate right) => false;
        public static bool operator !=(Rotate left, Rotate right) => false;
    }
    public readonly struct Scale
    {
        public UnityEngine.Vector3 value { get; }
        public Scale(UnityEngine.Vector2 value) { }
        public Scale(UnityEngine.Vector3 value) { this.value = value; }
        public static Scale None() => default;
        public bool Equals(Scale other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static implicit operator Scale(UnityEngine.Vector2 value) => new(value);
        public static implicit operator Scale(UnityEngine.Vector3 value) => new(value);
        public static bool operator ==(Scale left, Scale right) => false;
        public static bool operator !=(Scale left, Scale right) => false;
    }
    public readonly struct StyleTranslate : IStyleValue<Translate>
    {
        public Translate value { get; }
        public StyleKeyword keyword { get; }
        public StyleTranslate(StyleKeyword keyword) { this.keyword = keyword; }
        public StyleTranslate(Translate value) { this.value = value; }
        public static implicit operator StyleTranslate(StyleKeyword value) => new(value);
        public static implicit operator StyleTranslate(Translate value) => new(value);
        public static implicit operator StyleTranslate(UnityEngine.Vector2 value) => new((Translate)value);
        public static implicit operator StyleTranslate(UnityEngine.Vector3 value) => new((Translate)value);
        public bool Equals(StyleTranslate other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleTranslate left, StyleTranslate right) => false;
        public static bool operator !=(StyleTranslate left, StyleTranslate right) => false;
    }
    public readonly struct StyleRotate : IStyleValue<Rotate>
    {
        public Rotate value { get; }
        public StyleKeyword keyword { get; }
        public StyleRotate(StyleKeyword keyword) { this.keyword = keyword; }
        public StyleRotate(Rotate value) { this.value = value; }
        public StyleRotate(UnityEngine.Quaternion value) { }
        public static implicit operator StyleRotate(StyleKeyword value) => new(value);
        public static implicit operator StyleRotate(Rotate value) => new(value);
        public static implicit operator StyleRotate(UnityEngine.Quaternion value) => new(value);
        public bool Equals(StyleRotate other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleRotate left, StyleRotate right) => false;
        public static bool operator !=(StyleRotate left, StyleRotate right) => false;
    }
    public readonly struct StyleScale : IStyleValue<Scale>
    {
        public Scale value { get; }
        public StyleKeyword keyword { get; }
        public StyleScale(StyleKeyword keyword) { this.keyword = keyword; }
        public StyleScale(Scale value) { this.value = value; }
        public StyleScale(UnityEngine.Vector2 value) { }
        public static implicit operator StyleScale(StyleKeyword value) => new(value);
        public static implicit operator StyleScale(Scale value) => new(value);
        public static implicit operator StyleScale(UnityEngine.Vector2 value) => new(value);
        public static implicit operator StyleScale(UnityEngine.Vector3 value) => new(new Scale(value));
        public bool Equals(StyleScale other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleScale left, StyleScale right) => false;
        public static bool operator !=(StyleScale left, StyleScale right) => false;
    }
    public readonly struct StyleTransformOrigin : IStyleValue<TransformOrigin>
    {
        public TransformOrigin value { get; }
        public StyleKeyword keyword { get; }
        public StyleTransformOrigin(StyleKeyword keyword) { this.keyword = keyword; }
        public StyleTransformOrigin(TransformOrigin value) { this.value = value; }
        public static implicit operator StyleTransformOrigin(StyleKeyword value) => new(value);
        public static implicit operator StyleTransformOrigin(TransformOrigin value) => new(value);
        public bool Equals(StyleTransformOrigin other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleTransformOrigin left, StyleTransformOrigin right) => false;
        public static bool operator !=(StyleTransformOrigin left, StyleTransformOrigin right) => false;
    }
    public readonly struct StyleTextShadow : IStyleValue<TextShadow>
    {
        public TextShadow value { get; }
        public StyleKeyword keyword { get; }
        public StyleTextShadow(StyleKeyword keyword) { this.keyword = keyword; }
        public StyleTextShadow(TextShadow value) { this.value = value; }
        public static implicit operator StyleTextShadow(StyleKeyword value) => new(value);
        public static implicit operator StyleTextShadow(TextShadow value) => new(value);
        public bool Equals(StyleTextShadow other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleTextShadow left, StyleTextShadow right) => false;
        public static bool operator !=(StyleTextShadow left, StyleTextShadow right) => false;
    }
    public readonly struct StyleLength : IStyleValue<Length>
    {
        public Length value { get; }
        public StyleKeyword keyword { get; }
        public StyleLength(float value) { this.value = new Length(value); }
        public StyleLength(Length value) { this.value = value; }
        public StyleLength(StyleKeyword keyword) { this.keyword = keyword; }
        public static implicit operator StyleLength(float value) => new(value);
        public static implicit operator StyleLength(Length value) => new(value);
        public static implicit operator StyleLength(StyleKeyword keyword) => new(keyword);
        public bool Equals(StyleLength other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleLength left, StyleLength right) => false;
        public static bool operator !=(StyleLength left, StyleLength right) => false;
    }
    public readonly struct StyleFloat : IStyleValue<float>
    {
        public float value { get; }
        public StyleKeyword keyword { get; }
        public StyleFloat(float value) { this.value = value; }
        public StyleFloat(StyleKeyword keyword) { this.keyword = keyword; }
        public static implicit operator StyleFloat(float value) => new(value);
        public static implicit operator StyleFloat(StyleKeyword keyword) => new(keyword);
        public bool Equals(StyleFloat other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleFloat left, StyleFloat right) => false;
        public static bool operator !=(StyleFloat left, StyleFloat right) => false;
    }
    public readonly struct StyleInt : IStyleValue<int>
    {
        public int value { get; }
        public StyleKeyword keyword { get; }
        public StyleInt(int value) { this.value = value; }
        public StyleInt(StyleKeyword keyword) { this.keyword = keyword; }
        public static implicit operator StyleInt(int value) => new(value);
        public static implicit operator StyleInt(StyleKeyword value) => new(value);
        public bool Equals(StyleInt other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleInt left, StyleInt right) => false;
        public static bool operator !=(StyleInt left, StyleInt right) => false;
    }
    public readonly struct StyleColor : IStyleValue<UnityEngine.Color>
    {
        public UnityEngine.Color value { get; }
        public StyleKeyword keyword { get; }
        public StyleColor(UnityEngine.Color value) { this.value = value; }
        public StyleColor(StyleKeyword keyword) { this.keyword = keyword; }
        public static implicit operator StyleColor(UnityEngine.Color value) => new(value);
        public static implicit operator StyleColor(StyleKeyword keyword) => new(keyword);
        public bool Equals(StyleColor other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleColor left, StyleColor right) => false;
        public static bool operator !=(StyleColor left, StyleColor right) => false;
        public static bool operator ==(StyleColor left, UnityEngine.Color right) => false;
        public static bool operator !=(StyleColor left, UnityEngine.Color right) => false;
    }
    public readonly struct Background
    {
        public UnityEngine.Texture2D texture { get; }
        public UnityEngine.Sprite sprite { get; }
        public UnityEngine.RenderTexture renderTexture { get; }
        public VectorImage vectorImage { get; }
        public Background(UnityEngine.Texture2D texture) { this.texture = texture; }
        public static Background FromTexture2D(UnityEngine.Texture2D texture) => new();
        public static Background FromSprite(UnityEngine.Sprite sprite) => new();
        public static Background FromRenderTexture(UnityEngine.RenderTexture value) => new();
        public static Background FromVectorImage(VectorImage value) => new();
        public bool IsEmpty() => texture == null && sprite == null;
        public UnityEngine.Object GetSelectedImage() => sprite != null ? sprite : texture;
        public bool Equals(Background other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static implicit operator Background(UnityEngine.Texture2D value) => new(value);
        public static bool operator ==(Background left, Background right) => false;
        public static bool operator !=(Background left, Background right) => false;
    }
    public readonly struct StyleBackground : IStyleValue<Background>
    {
        public Background value { get; }
        public StyleKeyword keyword { get; }
        public StyleBackground(Background value) { this.value = value; }
        public StyleBackground(UnityEngine.Texture2D value) { }
        public StyleBackground(UnityEngine.Sprite value) { }
        public StyleBackground(VectorImage value) { }
        public StyleBackground(StyleKeyword keyword) { this.keyword = keyword; }
        public static implicit operator StyleBackground(Background value) => new(value);
        public static implicit operator StyleBackground(UnityEngine.Texture2D value) => new(value);
        public static implicit operator StyleBackground(UnityEngine.Sprite value) => new(value);
        public static implicit operator StyleBackground(StyleKeyword keyword) => new(keyword);
        public bool Equals(StyleBackground other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleBackground left, StyleBackground right) => false;
        public static bool operator !=(StyleBackground left, StyleBackground right) => false;
    }
    public readonly struct StyleEnum<T> : IStyleValue<T> where T : struct, System.Enum
    {
        public T value { get; }
        public StyleKeyword keyword { get; }
        public StyleEnum(T value) { this.value = value; }
        public StyleEnum(StyleKeyword keyword) { this.keyword = keyword; }
        public static implicit operator StyleEnum<T>(T value) => new(value);
        public static implicit operator StyleEnum<T>(StyleKeyword keyword) => new(keyword);
        public bool Equals(StyleEnum<T> other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleEnum<T> left, StyleEnum<T> right) => false;
        public static bool operator !=(StyleEnum<T> left, StyleEnum<T> right) => false;
    }
    public readonly struct Ratio
    {
        public float value { get; }
        public Ratio(float value) { this.value = value; }
        public static Ratio Auto() => new();
        public bool IsAuto() => false;
        public bool Equals(Ratio other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static implicit operator Ratio(float value) => new(value);
        public static implicit operator float(Ratio value) => value.value;
        public static bool operator ==(Ratio left, Ratio right) => false;
        public static bool operator !=(Ratio left, Ratio right) => false;
    }
    public readonly struct StyleRatio
    {
        public Ratio value { get; }
        public StyleKeyword keyword { get; }
        public StyleRatio(Ratio value) { this.value = value; }
        public StyleRatio(StyleKeyword keyword) { this.keyword = keyword; }
        public static StyleRatio Auto() => new(StyleKeyword.Auto);
        public bool Equals(StyleRatio other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static implicit operator StyleRatio(Ratio value) => new(value);
        public static implicit operator StyleRatio(float value) => new(new Ratio(value));
        public static implicit operator StyleRatio(StyleKeyword keyword) => new(keyword);
        public static implicit operator Ratio(StyleRatio value) => value.value;
        public static implicit operator float(StyleRatio value) => value.value.value;
        public static implicit operator StyleKeyword(StyleRatio value) => value.keyword;
        public static bool operator ==(StyleRatio left, StyleRatio right) => false;
        public static bool operator !=(StyleRatio left, StyleRatio right) => false;
    }
    public readonly struct BackgroundPosition
    {
        public BackgroundPositionKeyword keyword { get; }
        public Length offset { get; }
        public BackgroundPosition(BackgroundPositionKeyword keyword) { this.keyword = keyword; }
        public BackgroundPosition(BackgroundPositionKeyword keyword, Length offset)
        { this.keyword = keyword; this.offset = offset; }
        public bool Equals(BackgroundPosition other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(BackgroundPosition left, BackgroundPosition right) => false;
        public static bool operator !=(BackgroundPosition left, BackgroundPosition right) => false;
    }
    public readonly struct BackgroundRepeat
    {
        public Repeat x { get; }
        public Repeat y { get; }
        public BackgroundRepeat(Repeat x, Repeat y) { this.x = x; this.y = y; }
        public bool Equals(BackgroundRepeat other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(BackgroundRepeat left, BackgroundRepeat right) => false;
        public static bool operator !=(BackgroundRepeat left, BackgroundRepeat right) => false;
    }
    public readonly struct BackgroundSize
    {
        public BackgroundSizeType sizeType { get; }
        public Length x { get; }
        public Length y { get; }
        public BackgroundSize(BackgroundSizeType type) { sizeType = type; }
        public BackgroundSize(Length x, Length y) { this.x = x; this.y = y; }
        public bool Equals(BackgroundSize other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(BackgroundSize left, BackgroundSize right) => false;
        public static bool operator !=(BackgroundSize left, BackgroundSize right) => false;
    }
    public static class BackgroundPropertyHelper
    {
        public static BackgroundPosition ConvertScaleModeToBackgroundPosition(UnityEngine.ScaleMode mode) => default;
        public static BackgroundRepeat ConvertScaleModeToBackgroundRepeat(UnityEngine.ScaleMode mode) => default;
        public static BackgroundSize ConvertScaleModeToBackgroundSize(UnityEngine.ScaleMode mode) => default;
        public static UnityEngine.ScaleMode ResolveUnityBackgroundScaleMode(
            BackgroundPosition x, BackgroundPosition y, BackgroundRepeat repeat,
            BackgroundSize size, out bool valid) { valid = false; return default; }
    }
    public readonly struct StyleBackgroundPosition
    {
        public BackgroundPosition value { get; }
        public StyleKeyword keyword { get; }
        public StyleBackgroundPosition(BackgroundPosition value) { this.value = value; }
        public StyleBackgroundPosition(StyleKeyword keyword) { this.keyword = keyword; }
        public static implicit operator StyleBackgroundPosition(BackgroundPosition value) => new(value);
        public static implicit operator StyleBackgroundPosition(StyleKeyword keyword) => new(keyword);
        public bool Equals(StyleBackgroundPosition other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleBackgroundPosition left, StyleBackgroundPosition right) => false;
        public static bool operator !=(StyleBackgroundPosition left, StyleBackgroundPosition right) => false;
    }
    public readonly struct StyleBackgroundRepeat
    {
        public BackgroundRepeat value { get; }
        public StyleKeyword keyword { get; }
        public StyleBackgroundRepeat(BackgroundRepeat value) { this.value = value; }
        public StyleBackgroundRepeat(StyleKeyword keyword) { this.keyword = keyword; }
        public static implicit operator StyleBackgroundRepeat(BackgroundRepeat value) => new(value);
        public static implicit operator StyleBackgroundRepeat(StyleKeyword keyword) => new(keyword);
        public bool Equals(StyleBackgroundRepeat other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleBackgroundRepeat left, StyleBackgroundRepeat right) => false;
        public static bool operator !=(StyleBackgroundRepeat left, StyleBackgroundRepeat right) => false;
    }
    public readonly struct StyleBackgroundSize
    {
        public BackgroundSize value { get; }
        public StyleKeyword keyword { get; }
        public StyleBackgroundSize(BackgroundSize value) { this.value = value; }
        public StyleBackgroundSize(StyleKeyword keyword) { this.keyword = keyword; }
        public static implicit operator StyleBackgroundSize(BackgroundSize value) => new(value);
        public static implicit operator StyleBackgroundSize(StyleKeyword keyword) => new(keyword);
        public bool Equals(StyleBackgroundSize other) => false;
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(StyleBackgroundSize left, StyleBackgroundSize right) => false;
        public static bool operator !=(StyleBackgroundSize left, StyleBackgroundSize right) => false;
    }
    public enum DisplayStyle { Flex = 0, None = 1 }
    public enum FlexDirection { Column = 0, ColumnReverse = 1, Row = 2, RowReverse = 3 }
    public enum Align { Auto = 0, FlexStart = 1, Center = 2, FlexEnd = 3, Stretch = 4 }
    public enum Justify { FlexStart = 0, Center = 1, FlexEnd = 2,
        SpaceBetween = 3, SpaceAround = 4, SpaceEvenly = 5 }
    public enum Position { Relative = 0, Absolute = 1 }
    public enum Overflow { Visible = 0, Hidden = 1 }
    public enum Visibility { Visible = 0, Hidden = 1 }
    public enum WhiteSpace { Normal = 0, NoWrap = 1, Pre = 2, PreWrap = 3 }
    public enum Wrap { NoWrap = 0, Wrap = 1, WrapReverse = 2 }
    public enum TextOverflow { Clip = 0, Ellipsis = 1 }
    public enum TrickleDown { NoTrickleDown, TrickleDown }
    public enum ScrollViewMode { Vertical, Horizontal, VerticalAndHorizontal }
    public enum ScrollerVisibility { Auto, AlwaysVisible, Hidden }
    public enum SliderDirection { Horizontal, Vertical }
    public enum PanelScaleMode { ConstantPixelSize, ConstantPhysicalSize, ScaleWithScreenSize }
    public enum PanelScreenMatchMode { MatchWidthOrHeight, Shrink, Expand }
    public struct TimerState
    {
        public long deltaTime { get; }
        public long now { get; }
        public long start { get; }
        public bool Equals(TimerState other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(TimerState left, TimerState right) => false;
        public static bool operator !=(TimerState left, TimerState right) => true;
    }
    public interface IVisualElementScheduledItem
    {
        VisualElement element { get; }
        bool isActive { get; }
        IVisualElementScheduledItem Every(long intervalMs);
        IVisualElementScheduledItem StartingIn(long delayMs);
        IVisualElementScheduledItem ForDuration(long durationMs);
        IVisualElementScheduledItem Until(System.Func<bool> stopCondition);
        void ExecuteLater(long delayMs);
        void Pause();
        void Resume();
    }
    public interface IVisualElementScheduler
    {
        IVisualElementScheduledItem Execute(System.Action callback);
        IVisualElementScheduledItem Execute(System.Action<TimerState> callback);
    }
    public interface IStyle
    {
        StyleColor unityBackgroundImageTintColor { get; set; }
        StyleEnum<UnityEngine.ScaleMode> unityBackgroundScaleMode { get; set; }
        StyleColor unityTextOutlineColor { get; set; }
        StyleFloat unityTextOutlineWidth { get; set; }
        object filter { get; set; }
        StyleEnum<EditorTextRenderingMode> unityEditorTextRenderingMode { get; set; }
        StyleEnum<OverflowClipBox> unityOverflowClipBox { get; set; }
        StyleLength unityParagraphSpacing { get; set; }
        StyleEnum<TextOverflowPosition> unityTextOverflowPosition { get; set; }
        StyleCursor cursor { get; set; }
        StyleFont unityFont { get; set; }
        StyleFontDefinition unityFontDefinition { get; set; }
        StyleTextAutoSize unityTextAutoSize { get; set; }
        StyleList<TimeValue> transitionDelay { get; set; }
        StyleList<TimeValue> transitionDuration { get; set; }
        StyleList<StylePropertyName> transitionProperty { get; set; }
        StyleList<EasingFunction> transitionTimingFunction { get; set; }
        StyleTextShadow textShadow { get; set; }
        StyleTransformOrigin transformOrigin { get; set; }
        StyleTranslate translate { get; set; }
        StyleRotate rotate { get; set; }
        StyleScale scale { get; set; }
        StyleRatio aspectRatio { get; set; }
        StyleBackgroundPosition backgroundPositionX { get; set; }
        StyleBackgroundPosition backgroundPositionY { get; set; }
        StyleBackgroundRepeat backgroundRepeat { get; set; }
        StyleBackgroundSize backgroundSize { get; set; }
        StyleBackground backgroundImage { get; set; }
        StyleEnum<Align> alignContent { get; set; }
        StyleEnum<Align> alignItems { get; set; }
        StyleEnum<Align> alignSelf { get; set; }
        StyleLength width { get; set; }
        StyleLength height { get; set; }
        StyleLength minWidth { get; set; }
        StyleLength minHeight { get; set; }
        StyleLength maxWidth { get; set; }
        StyleLength maxHeight { get; set; }
        StyleLength left { get; set; }
        StyleLength right { get; set; }
        StyleLength top { get; set; }
        StyleLength bottom { get; set; }
        StyleLength marginLeft { get; set; }
        StyleLength marginRight { get; set; }
        StyleLength marginTop { get; set; }
        StyleLength marginBottom { get; set; }
        StyleLength paddingLeft { get; set; }
        StyleLength paddingRight { get; set; }
        StyleLength paddingTop { get; set; }
        StyleLength paddingBottom { get; set; }
        StyleLength borderTopLeftRadius { get; set; }
        StyleLength borderTopRightRadius { get; set; }
        StyleLength borderBottomLeftRadius { get; set; }
        StyleLength borderBottomRightRadius { get; set; }
        StyleLength fontSize { get; set; }
        StyleLength letterSpacing { get; set; }
        StyleLength wordSpacing { get; set; }
        StyleLength flexBasis { get; set; }
        StyleFloat opacity { get; set; }
        StyleFloat flexGrow { get; set; }
        StyleFloat flexShrink { get; set; }
        StyleFloat borderLeftWidth { get; set; }
        StyleFloat borderRightWidth { get; set; }
        StyleFloat borderTopWidth { get; set; }
        StyleFloat borderBottomWidth { get; set; }
        StyleColor backgroundColor { get; set; }
        StyleColor color { get; set; }
        StyleColor borderLeftColor { get; set; }
        StyleColor borderRightColor { get; set; }
        StyleColor borderTopColor { get; set; }
        StyleColor borderBottomColor { get; set; }
        StyleEnum<DisplayStyle> display { get; set; }
        StyleEnum<FlexDirection> flexDirection { get; set; }
        StyleEnum<Wrap> flexWrap { get; set; }
        StyleEnum<Justify> justifyContent { get; set; }
        StyleEnum<Position> position { get; set; }
        StyleEnum<Overflow> overflow { get; set; }
        StyleEnum<Visibility> visibility { get; set; }
        StyleEnum<WhiteSpace> whiteSpace { get; set; }
        StyleEnum<TextOverflow> textOverflow { get; set; }
        void Clear(bool clearSourceAssetStyles = true);
        object unityMaterial { get; set; }
        object unityTextGenerator { get; set; }
        object unitySliceBottom { get; set; }
        object unitySliceLeft { get; set; }
        object unitySliceRight { get; set; }
        object unitySliceScale { get; set; }
        object unitySliceTop { get; set; }
        object unitySliceType { get; set; }
    }
    public interface IResolvedStyle
    {
        UnityEngine.Color unityBackgroundImageTintColor { get; }
        StyleEnum<UnityEngine.ScaleMode> unityBackgroundScaleMode { get; }
        UnityEngine.Color unityTextOutlineColor { get; }
        float unityTextOutlineWidth { get; }
        UnityEngine.FontStyle unityFontStyleAndWeight { get; }
        UnityEngine.TextAnchor unityTextAlign { get; }
        object filter { get; }
        EditorTextRenderingMode unityEditorTextRenderingMode { get; }
        float unityParagraphSpacing { get; }
        TextOverflowPosition unityTextOverflowPosition { get; }
        UnityEngine.Font unityFont { get; }
        FontDefinition unityFontDefinition { get; }
        System.Collections.Generic.IEnumerable<TimeValue> transitionDelay { get; }
        System.Collections.Generic.IEnumerable<TimeValue> transitionDuration { get; }
        System.Collections.Generic.IEnumerable<StylePropertyName> transitionProperty { get; }
        System.Collections.Generic.IEnumerable<EasingFunction> transitionTimingFunction { get; }
        TransformOrigin transformOrigin { get; }
        Translate translate { get; }
        Rotate rotate { get; }
        Scale scale { get; }
        Ratio aspectRatio { get; }
        BackgroundPosition backgroundPositionX { get; }
        BackgroundPosition backgroundPositionY { get; }
        BackgroundRepeat backgroundRepeat { get; }
        BackgroundSize backgroundSize { get; }
        Background backgroundImage { get; }
        Align alignContent { get; }
        Align alignItems { get; }
        Align alignSelf { get; }
        float width { get; }
        float height { get; }
        float left { get; }
        float right { get; }
        float top { get; }
        float bottom { get; }
        float marginLeft { get; }
        float marginRight { get; }
        float marginTop { get; }
        float marginBottom { get; }
        float paddingLeft { get; }
        float paddingRight { get; }
        float paddingTop { get; }
        float paddingBottom { get; }
        float opacity { get; }
        float flexGrow { get; }
        float flexShrink { get; }
        float borderLeftWidth { get; }
        float borderRightWidth { get; }
        float borderTopWidth { get; }
        float borderBottomWidth { get; }
        UnityEngine.Color backgroundColor { get; }
        UnityEngine.Color color { get; }
        DisplayStyle display { get; }
        FlexDirection flexDirection { get; }
        Wrap flexWrap { get; }
        Justify justifyContent { get; }
        Position position { get; }
        Visibility visibility { get; }
        WhiteSpace whiteSpace { get; }
        TextOverflow textOverflow { get; }
        object unityMaterial { get; }
        object unityTextGenerator { get; }
        object unitySliceBottom { get; }
        object unitySliceLeft { get; }
        object unitySliceRight { get; }
        object unitySliceScale { get; }
        object unitySliceTop { get; }
        object unitySliceType { get; }
    }
    public enum PickingMode { Position, Ignore }
    public delegate void EventCallback<T>(T evt) where T : EventBase;
    public delegate void EventCallback<T, TUserArgs>(T evt, TUserArgs userArgs) where T : EventBase;
    public interface IEventHandler
    {
        void HandleEvent(EventBase evt);
        bool HasBubbleUpHandlers();
        bool HasTrickleDownHandlers();
        void SendEvent(EventBase evt);
    }
    public abstract class CallbackEventHandler : IEventHandler
    {
        public void HandleEvent(EventBase evt) { }
        public bool HasBubbleUpHandlers() => false;
        public bool HasTrickleDownHandlers() => false;
        public void SendEvent(EventBase evt) { }
        public void UnregisterAllRemovableCallbacks() { }
        public void RegisterCallback<T>(EventCallback<T> callback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where T : EventBase { }
        public void RegisterCallback<T>(EventCallback<T> callback, CallbackOptions options)
            where T : EventBase { }
        public void RegisterCallbackOnce<T>(EventCallback<T> callback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where T : EventBase { }
        public void UnregisterCallback<T>(EventCallback<T> callback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where T : EventBase { }
        public void UnregisterCallback<T>(EventCallback<T> callback, CallbackOptions options)
            where T : EventBase { }
        public void RegisterCallback<T, TUserArgs>(EventCallback<T, TUserArgs> callback,
            TUserArgs userArgs, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where T : EventBase { }
        public void RegisterCallback<T, TUserArgs>(EventCallback<T, TUserArgs> callback,
            TUserArgs userArgs, CallbackOptions options) where T : EventBase { }
        public void RegisterCallbackOnce<T, TUserArgs>(EventCallback<T, TUserArgs> callback,
            TUserArgs userArgs, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where T : EventBase { }
        public void UnregisterCallback<T, TUserArgs>(EventCallback<T, TUserArgs> callback,
            TUserArgs userArgs, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where T : EventBase { }
        public void UnregisterCallback<T, TUserArgs>(EventCallback<T, TUserArgs> callback,
            TUserArgs userArgs, CallbackOptions options) where T : EventBase { }
    }
    public class StyleSheet : UnityEngine.ScriptableObject
    {
        public UnityEngine.Hash128 contentHash { get; }
        public bool importedWithErrors { get; }
        public bool importedWithWarnings { get; }
        public System.Collections.Generic.List<StyleSheet> stylesheets { get; } = new();
    }
    public class ThemeStyleSheet : StyleSheet { }
    public readonly struct CustomStyleProperty<T> : System.IEquatable<CustomStyleProperty<T>>
    {
        public CustomStyleProperty(string name) { this.name = name; }
        public string name { get; }
        public bool Equals(CustomStyleProperty<T> other) => name == other.name;
        public override bool Equals(object? other) => other is CustomStyleProperty<T> property && Equals(property);
        public override int GetHashCode() => name?.GetHashCode() ?? 0;
        public static bool operator ==(CustomStyleProperty<T> left, CustomStyleProperty<T> right) => left.Equals(right);
        public static bool operator !=(CustomStyleProperty<T> left, CustomStyleProperty<T> right) => !left.Equals(right);
    }
    public interface ICustomStyle
    {
        bool TryGetValue<T>(CustomStyleProperty<T> property, out T value);
        bool TryGetValue(CustomStyleProperty<bool> property, out bool value);
        bool TryGetValue(CustomStyleProperty<int> property, out int value);
        bool TryGetValue(CustomStyleProperty<float> property, out float value);
        bool TryGetValue(CustomStyleProperty<string> property, out string value);
        bool TryGetValue(CustomStyleProperty<UnityEngine.Color> property, out UnityEngine.Color value);
        bool TryGetValue(CustomStyleProperty<UnityEngine.Sprite> property, out UnityEngine.Sprite value);
        bool TryGetValue(CustomStyleProperty<UnityEngine.Texture2D> property, out UnityEngine.Texture2D value);
        bool TryGetValue(CustomStyleProperty<VectorImage> property, out VectorImage value);
    }
    public class VisualElementStyleSheetSet
    {
        public int count { get; }
        public StyleSheet this[int index] => null!;
        public void Add(StyleSheet sheet) { }
        public void Insert(int index, StyleSheet sheet) { }
        public bool Remove(StyleSheet sheet) => false;
        public bool Contains(StyleSheet sheet) => false;
        public void Clear() { }
        public bool Equals(VisualElementStyleSheetSet other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(VisualElementStyleSheetSet left,
            VisualElementStyleSheetSet right) => false;
        public static bool operator !=(VisualElementStyleSheetSet left,
            VisualElementStyleSheetSet right) => true;
    }
    public interface IFocusRing { }
    public class FocusController
    {
        public FocusController(IFocusRing focusRing) { }
        public Focusable focusedElement { get; }
        public void IgnoreEvent(EventBase evt) { }
    }
    public class FocusChangeDirection
    {
        public static FocusChangeDirection none { get; }
        public static FocusChangeDirection unspecified { get; }
        public static implicit operator int(FocusChangeDirection direction) => 0;
    }
    public interface IPanel
    {
        object contextType { get; }
        object contextualMenuManager { get; }
        object dispatcher { get; }
        bool isDirty { get; }
        VisualElement visualTree { get; }
        FocusController focusController { get; }
        float scaledPixelsPerPoint { get; }
        VisualElement Pick(UnityEngine.Vector2 point);
        VisualElement PickAll(UnityEngine.Vector2 point,
            System.Collections.Generic.List<VisualElement> picked);
    }
    public interface IRuntimePanel : IPanel
    {
        PanelSettings panelSettings { get; }
        UnityEngine.GameObject selectableGameObject { get; }
    }
    public static class RuntimePanelUtils
    {
        public static UnityEngine.Vector2 CameraTransformWorldToPanel(IPanel panel,
            UnityEngine.Vector3 worldPosition, UnityEngine.Camera camera) => default;
        public static UnityEngine.Rect CameraTransformWorldToPanelRect(IPanel panel,
            UnityEngine.Vector3 worldPosition, UnityEngine.Vector2 size,
            UnityEngine.Camera camera) => default;
        public static void ResetDynamicAtlas(IPanel panel) { }
        public static void ResetRenderer(IPanel panel) { }
        public static UnityEngine.Vector2 ScreenToPanel(IPanel panel,
            UnityEngine.Vector2 screenPosition) => default;
        public static void SetTextureDirty(IPanel panel, UnityEngine.Texture2D texture) { }
    }
    public static class PointerCaptureHelper
    {
        public static void CapturePointer(IEventHandler handler, int pointerId) { }
        public static void ReleasePointer(IEventHandler handler, int pointerId) { }
        public static void ReleasePointer(IPanel panel, int pointerId) { }
        public static bool HasPointerCapture(IEventHandler handler, int pointerId) => false;
        public static IEventHandler GetCapturingElement(IPanel panel, int pointerId) => null!;
    }
    public static class MouseCaptureController
    {
        public static void CaptureMouse(IEventHandler handler) { }
        public static void ReleaseMouse() { }
        public static void ReleaseMouse(IEventHandler handler) { }
        public static bool HasMouseCapture(IEventHandler handler) => false;
        public static bool IsMouseCaptured() => false;
    }
    public class Focusable : CallbackEventHandler
    {
        public bool focusable { get; set; }
        public bool canGrabFocus { get; }
        public bool delegatesFocus { get; set; }
        public FocusController focusController { get; }
        public int tabIndex { get; set; }
        public virtual void Focus() { }
    }
    public partial class VisualElement : Focusable
    {
        public static string disabledUssClassName { get; }
        public VisualElementStyleSheetSet styleSheets { get; }
        public struct Hierarchy
        {
            public VisualElement parent { get; }
            public int childCount { get; }
            public VisualElement this[int index] { get => null!; }
            public void Add(VisualElement child) { }
            public void Insert(int index, VisualElement child) { }
            public bool Remove(VisualElement child) => false;
            public void RemoveAt(int index) { }
            public void Clear() { }
            public System.Collections.Generic.IEnumerable<VisualElement> Children()
                => System.Array.Empty<VisualElement>();
            public VisualElement ElementAt(int index) => null!;
            public int IndexOf(VisualElement child) => -1;
            public void Sort(System.Comparison<VisualElement> comparison) { }
            public bool Equals(Hierarchy other) => false;
            public override bool Equals(object? other) => false;
            public override int GetHashCode() => 0;
            public static bool operator ==(Hierarchy left, Hierarchy right) => false;
            public static bool operator !=(Hierarchy left, Hierarchy right) => true;
        }
        public string name { get; set; } = "";
        public IStyle style { get; }
        public IResolvedStyle resolvedStyle { get; }
        public ICustomStyle customStyle { get; }
        public PickingMode pickingMode { get; set; }
        public string tooltip { get; set; } = "";
        public string viewDataKey { get; set; } = "";
        public bool hasActivePseudoState { get; }
        public bool hasInactivePseudoState { get; }
        public bool hasRootPseudoState { get; }
        public LanguageDirection languageDirection { get; set; }
        public UsageHints usageHints { get; set; }
        public bool hasCheckedPseudoState { get; }
        public bool hasDisabledPseudoState { get; }
        public bool hasEnabledPseudoState { get; }
        public bool hasFocusPseudoState { get; }
        public bool hasHoverPseudoState { get; }
        public VisualElement parent { get; }
        public IPanel panel { get; }
        public int childCount { get; }
        public VisualElement contentContainer { get; }
        public Hierarchy hierarchy { get; }
        public bool enabledSelf { get; }
        public bool enabledInHierarchy { get; }
        public float scaledPixelsPerPoint { get; }
        public UnityEngine.Rect layout { get; }
        public UnityEngine.Rect worldBound { get; }
        public UnityEngine.Rect localBound { get; }
        public UnityEngine.Rect contentRect { get; }
        public object userData { get; set; } = null!;
        public IVisualElementScheduler schedule { get; }
        public void Add(VisualElement child) { }
        public void Insert(int index, VisualElement child) { }
        public bool Remove(VisualElement child) => false;
        public void RemoveAt(int index) { }
        public void Clear() { }
        public System.Collections.Generic.IEnumerable<VisualElement> Children()
            => System.Array.Empty<VisualElement>();
        public int IndexOf(VisualElement child) => -1;
        public VisualElement ElementAt(int index) => null!;
        public VisualElement this[int index] => ElementAt(index);
        public bool Contains(VisualElement child) => false;
        public VisualElement FindCommonAncestor(VisualElement other) => null!;
        public T GetFirstAncestorOfType<T>() where T : VisualElement => null!;
        public T GetFirstOfType<T>() where T : VisualElement => null!;
        public object FindAncestorUserData() => null!;
        public void Sort(System.Comparison<VisualElement> comparison) { }
        public void BringToFront() { }
        public void SendToBack() { }
        public void PlaceBehind(VisualElement other) { }
        public void PlaceInFront(VisualElement other) { }
        public void RemoveFromHierarchy() { }
        public void SetEnabled(bool value) { }
        public void SetActivePseudoState(bool value) { }
        public void SetCheckedPseudoState(bool value) { }
        public bool ContainsPoint(UnityEngine.Vector2 point) => false;
        public bool Overlaps(UnityEngine.Rect rectangle) => false;
        public override void Focus() { }
        public void Blur() { }
        public void CapturePointer(int pointerId) { }
        public void ReleasePointer(int pointerId) { }
        public bool HasPointerCapture(int pointerId) => false;
        public void AddToClassList(string name) { }
        public void RemoveFromClassList(string name) { }
        public bool ClassListContains(string name) => false;
        public void ClearClassList() { }
        public System.Collections.Generic.IEnumerable<string> GetClasses()
            => System.Array.Empty<string>();
        public void EnableInClassList(string name, bool value) { }
        public void ToggleInClassList(string name) { }
        public void MarkDirtyRepaint() { }
        public bool IsMarkedForRepaint() => false;
        public T Q<T>(string name = null, params string[] classes) where T : VisualElement => null;
        public VisualElement Q(string name = null, params string[] classes) => null;
        public UQueryBuilder<T> Query<T>(string name = null, params string[] classes)
            where T : VisualElement => default;
        public UQueryBuilder<VisualElement> Query(string name = null, params string[] classes)
            => default;
    }
    public class BindableElement : VisualElement
    {
        public string bindingPath { get; set; } = "";
    }
    public interface INotifyValueChanged<T>
    {
        T value { get; set; }
        void SetValueWithoutNotify(T value);
    }
    public interface IValueField<T> : INotifyValueChanged<T>
    {
        T ApplyInputDeviceDelta(UnityEngine.Vector3 delta, DeltaSpeed speed, T startValue);
        void StartDragging();
        void StopDragging();
    }
    public interface IMixedValueSupport { bool showMixedValue { get; set; } }
    public class BaseField<T> : BindableElement, INotifyValueChanged<T>, IMixedValueSupport
    {
        public string label { get; set; } = "";
        public VisualElement labelElement { get; }
        public bool showMixedValue { get; set; }
        public T value { get; set; } = default!;
        public virtual void SetValueWithoutNotify(T value) { }
    }
    public class BaseBoolField : BaseField<bool>
    {
        public BaseBoolField(string label = "") { this.label = label; }
        public string text { get; set; } = "";
        public bool toggleOnLabelClick { get; set; } = true;
    }
    public class BaseCompositeField<TValueType, TField, TFieldValue>
        : BaseField<TValueType> where TField : VisualElement
    {
        public bool isDelayed { get; set; }
    }
    public static class INotifyValueChangedExtensions
    {
        public static void RegisterValueChangedCallback<T>(
            this INotifyValueChanged<T> field, EventCallback<ChangeEvent<T>> callback) { }
        public static void UnregisterValueChangedCallback<T>(
            this INotifyValueChanged<T> field, EventCallback<ChangeEvent<T>> callback) { }
    }
    public interface IManipulator { }
    public static class VisualElementExtensions
    {
        public static UnityEngine.Vector2 LocalToWorld(this VisualElement element,
            UnityEngine.Vector2 point) => default;
        public static UnityEngine.Rect LocalToWorld(this VisualElement element,
            UnityEngine.Rect rect) => default;
        public static UnityEngine.Vector2 WorldToLocal(this VisualElement element,
            UnityEngine.Vector2 point) => default;
        public static UnityEngine.Rect WorldToLocal(this VisualElement element,
            UnityEngine.Rect rect) => default;
        public static UnityEngine.Vector2 ChangeCoordinatesTo(this VisualElement source,
            VisualElement destination, UnityEngine.Vector2 point) => default;
        public static UnityEngine.Rect ChangeCoordinatesTo(this VisualElement source,
            VisualElement destination, UnityEngine.Rect rect) => default;
        public static void StretchToParentSize(this VisualElement element) { }
        public static void StretchToParentWidth(this VisualElement element) { }
        public static void AddManipulator(this VisualElement element, IManipulator manipulator) { }
        public static void RemoveManipulator(this VisualElement element, IManipulator manipulator) { }
    }
    public static class UQuery { }
    public static class UQueryExtensions
    {
        public static VisualElement Q(this VisualElement element, string name,
            string className) => null!;
        public static VisualElement Q(this VisualElement element, string name,
            params string[] classes) => null!;
        public static T Q<T>(this VisualElement element, string name,
            string className) where T : VisualElement => null!;
        public static UQueryBuilder<VisualElement> Query(this VisualElement element)
            => default;
        public static UQueryBuilder<VisualElement> Query(this VisualElement element,
            string name, string className) => default;
    }
    public struct UQueryBuilder<T> where T : VisualElement
    {
        public UQueryBuilder(VisualElement root) { }
        public UQueryBuilder<T> Name(string name) => this;
        public UQueryBuilder<T> Class(string className) => this;
        public UQueryBuilder<T> Where(System.Func<T, bool> predicate) => this;
        public UQueryBuilder<T> Active() => this;
        public UQueryBuilder<T> Checked() => this;
        public UQueryBuilder<T> Enabled() => this;
        public UQueryBuilder<T> Focused() => this;
        public UQueryBuilder<T> Hovered() => this;
        public UQueryBuilder<T> Selected() => this;
        public UQueryBuilder<T> Visible() => this;
        public UQueryBuilder<T> NotActive() => this;
        public UQueryBuilder<T> NotChecked() => this;
        public UQueryBuilder<T> NotEnabled() => this;
        public UQueryBuilder<T> NotFocused() => this;
        public UQueryBuilder<T> NotHovered() => this;
        public UQueryBuilder<T> NotSelected() => this;
        public UQueryBuilder<T> NotVisible() => this;
        public UQueryBuilder<T2> OfType<T2>(string name = null, params string[] classes)
            where T2 : VisualElement => default;
        public UQueryBuilder<T2> Children<T2>(string name = null, params string[] classes)
            where T2 : VisualElement => default;
        public UQueryBuilder<T2> Descendents<T2>(string name = null, params string[] classes)
            where T2 : VisualElement => default;
        public UQueryState<T> Build() => default;
        public T First() => null!;
        public T Last() => null!;
        public T AtIndex(int index) => null!;
        public System.Collections.Generic.List<T> ToList() => new();
        public void ToList(System.Collections.Generic.List<T> destination) { }
        public void ForEach(System.Action<T> action) { }
        public System.Collections.Generic.List<T2> ForEach<T2>(System.Func<T, T2> transform)
            => new();
        public void ForEach<T2>(System.Collections.Generic.List<T2> destination,
            System.Func<T, T2> transform) { }
        public bool Equals(UQueryBuilder<T> other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public static implicit operator T(UQueryBuilder<T> query) => null!;
        public static bool operator ==(UQueryBuilder<T> left, UQueryBuilder<T> right) => false;
        public static bool operator !=(UQueryBuilder<T> left, UQueryBuilder<T> right) => true;
    }
    public struct UQueryState<T> where T : VisualElement
    {
        public struct Enumerator : System.IDisposable
        {
            public T Current { get; }
            public bool MoveNext() => false;
            public void Reset() { }
            public void Dispose() { }
        }
        public bool Equals(UQueryState<T> other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(UQueryState<T> left, UQueryState<T> right) => false;
        public static bool operator !=(UQueryState<T> left, UQueryState<T> right) => true;
        public T First() => null!;
        public T Last() => null!;
        public T AtIndex(int index) => null!;
        public System.Collections.Generic.List<T> ToList() => new();
        public void ToList(System.Collections.Generic.List<T> destination) { }
        public void ForEach(System.Action<T> action) { }
        public System.Collections.Generic.List<T2> ForEach<T2>(System.Func<T, T2> transform)
            => new();
        public void ForEach<T2>(System.Collections.Generic.List<T2> destination,
            System.Func<T, T2> transform) { }
        public UQueryState<T> RebuildOn(VisualElement root) => this;
        public Enumerator GetEnumerator() => default;
    }
    public class TemplateContainer : BindableElement
    {
        public TemplateContainer(string templateId = "") { }
        public string templateId { get; }
        public VisualTreeAsset templateSource { get; }
    }
    public class TextElement : BindableElement
    {
        public string text { get; set; } = "";
        public bool enableRichText { get; set; } = true;
        public bool emojiFallbackSupport { get; set; } = true;
        public bool parseEscapeSequences { get; set; }
        public bool displayTooltipWhenElided { get; set; }
        public bool isElided { get; }
        public void MarkDirtyText() { }
        public UnityEngine.Vector2 MeasureTextSize(string text, float width,
            VisualElement.MeasureMode widthMode, float height,
            VisualElement.MeasureMode heightMode) => default;
        public UnityEngine.Vector2 MeasureTextSize(string text, float width,
            VisualElement.MeasureMode widthMode, float height,
            VisualElement.MeasureMode heightMode, float? pixelsPerPoint) => default;
    }
    public class Label : TextElement { public Label() { } public Label(string text) { this.text = text; } }
    public class Box : VisualElement { public Box() { } }
    public class GroupBox : BindableElement
    {
        public GroupBox() { }
        public GroupBox(string text) { }
        public string text { get; set; } = "";
    }
    public class PopupWindow : TextElement
    {
        public PopupWindow() { }
        public PopupWindow(string text) { this.text = text; }
    }
    public enum HelpBoxMessageType { None = 0, Info = 1, Warning = 2, Error = 3 }
    public class HelpBox : VisualElement
    {
        public HelpBox() { }
        public HelpBox(string text, HelpBoxMessageType messageType) { }
        public string text { get; set; } = "";
        public HelpBoxMessageType messageType { get; set; }
    }
    public class Image : VisualElement
    {
        public Image() { }
        public UnityEngine.Texture image { get; set; } = null!;
        public UnityEngine.Sprite sprite { get; set; } = null!;
        public VectorImage vectorImage { get; set; } = null!;
        public UnityEngine.ScaleMode scaleMode { get; set; }
        public UnityEngine.Color tintColor { get; set; }
        public UnityEngine.Rect uv { get; set; }
        public UnityEngine.Rect sourceRect { get; set; }
    }
    public class Button : TextElement
    {
        public Button() { }
        public Button(System.Action clickEvent) { }
        public Button(Background iconImage, System.Action clickEvent) { }
        public event System.Action clicked { add { } remove { } }
        public event System.Action onClick { add { } remove { } }
        public Clickable clickable { get; set; } = null!;
        public Background iconImage { get; set; }
    }
    public struct ManipulatorActivationFilter
    {
        public int button { get; set; }
        public int clickCount { get; set; }
        public UnityEngine.EventModifiers modifiers { get; set; }
        public bool Matches(IMouseEvent evt) => false;
        public bool Matches(IPointerEvent evt) => false;
        public bool Equals(ManipulatorActivationFilter other) => false;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public static bool operator ==(ManipulatorActivationFilter left,
            ManipulatorActivationFilter right) => false;
        public static bool operator !=(ManipulatorActivationFilter left,
            ManipulatorActivationFilter right) => true;
    }
    public class Clickable
    {
        public Clickable(System.Action action) { }
        public Clickable(System.Action action, long delay, long interval) { }
        public Clickable(System.Action<EventBase> action) { }
        public event System.Action clicked { add { } remove { } }
        public event System.Action<EventBase> clickedWithEventInfo { add { } remove { } }
        public System.Collections.Generic.List<ManipulatorActivationFilter> activators { get; } = new();
        public UnityEngine.Vector2 lastMousePosition { get; }
    }
    public class EnumField : BaseField<System.Enum>
    {
        public EnumField() { }
        public EnumField(System.Enum value) { }
        public EnumField(string label, System.Enum value) { }
        public string label { get; set; } = "";
        public System.Enum value { get; set; } = null!;
        public string text { get; }
        public void Init(System.Enum value) { }
        public void Init(System.Enum value, bool includeObsoleteValues) { }
        public void SetValueWithoutNotify(System.Enum value) { }
    }
    public class Hash128Field : TextInputBaseField<UnityEngine.Hash128>
    {
        public Hash128Field() { maxLength = 32; }
        public Hash128Field(int maxLength) { this.maxLength = maxLength; }
        public Hash128Field(string label, int maxLength = 32)
            { this.label = label; this.maxLength = maxLength; }
    }
    public class RepeatButton : Button
    {
        public RepeatButton() { }
        public RepeatButton(System.Action clickEvent, long delay, long interval) { }
        public long delay { get; set; }
        public long interval { get; set; }
        public void SetAction(System.Action clickEvent, long delay, long interval) { }
    }
    public interface ITextSelection
    {
        UnityEngine.Color cursorColor { get; set; }
        UnityEngine.Color selectionColor { get; set; }
        UnityEngine.Vector2 cursorPosition { get; }
        UnityEngine.Vector2 GetCursorPositionFromStringIndex(int index);
        event System.Action<int> OnCursorIndexChange;
        event System.Action<int> OnSelectIndexChange;
        int cursorIndex { get; set; }
        int selectIndex { get; set; }
        bool isSelectable { get; }
        bool selectAllOnFocus { get; set; }
        bool selectAllOnMouseUp { get; set; }
        bool doubleClickSelectsWord { get; set; }
        bool tripleClickSelectsLine { get; set; }
        bool HasSelection();
        void SelectAll();
        void SelectNone();
        void SelectRange(int cursorIndex, int selectIndex);
        void MoveBackward();
        void MoveForward();
        void MoveWordBackward();
        void MoveWordForward();
        void MoveToEndOfPreviousWord();
        void MoveToStartOfNextWord();
        void MoveToParagraphStart();
        void MoveToParagraphEnd();
    }
    public interface ITextEdition
    {
        bool autoCorrection { get; set; }
        bool hideMobileInput { get; set; }
        bool hidePlaceholderOnFocus { get; set; }
        bool hideSoftKeyboard { get; set; }
        bool isDelayed { get; set; }
        bool isPassword { get; set; }
        bool isReadOnly { get; set; }
        UnityEngine.TouchScreenKeyboardType keyboardType { get; set; }
        char maskChar { get; set; }
        int maxLength { get; set; }
        string placeholder { get; set; }
        UnityEngine.TouchScreenKeyboard touchScreenKeyboard { get; }
    }
    public abstract class TextInputBaseField<T> : BaseField<T>, ITextSelection, ITextEdition
    {
        public event System.Action<int> OnCursorIndexChange
            { add { } remove { } }
        public event System.Action<int> OnSelectIndexChange
            { add { } remove { } }
        public UnityEngine.Vector2 GetCursorPositionFromStringIndex(int index) => default;
        public static string inputUssClassName => "";
        public static string labelUssClassName => "";
        public static string multilineInputUssClassName => "";
        public static string placeholderUssClassName => "";
        public static string singleLineInputUssClassName => "";
        public static string textInputUssName => "";
        public static string ussClassName => "";
        public bool autoCorrection { get; set; }
        public bool hidePlaceholderOnFocus { get; set; }
        public string placeholder { get; set; } = "";
        public bool isPassword { get; set; }
        public UnityEngine.Color cursorColor { get; set; }
        public UnityEngine.Vector2 cursorPosition { get; }
        public bool emojiFallbackSupport { get; set; } = true;
        public bool hideMobileInput { get; set; }
        public bool hideSoftKeyboard { get; set; }
        public UnityEngine.TouchScreenKeyboardType keyboardType { get; set; }
        public UnityEngine.Color selectionColor { get; set; }
        public ITextEdition textEdition { get; }
        public UnityEngine.TouchScreenKeyboard touchScreenKeyboard { get; }
        public ScrollerVisibility verticalScrollerVisibility { get; set; }
        public void SetVerticalScrollerVisibility(ScrollerVisibility visibility) { }
        public UnityEngine.Vector2 MeasureTextSize(string text, float width,
            VisualElement.MeasureMode widthMode, float height,
            VisualElement.MeasureMode heightMode) => default;
        public string label { get; set; } = "";
        public T value { get; set; } = default!;
        public string text { get; set; } = "";
        public ITextSelection textSelection => this;
        public int cursorIndex { get; set; }
        public int selectIndex { get; set; }
        public bool isSelectable => true;
        public bool selectAllOnFocus { get; set; } = true;
        public bool selectAllOnMouseUp { get; set; } = true;
        public bool doubleClickSelectsWord { get; set; } = true;
        public bool tripleClickSelectsLine { get; set; } = true;
        public bool isReadOnly { get; set; }
        public int maxLength { get; set; } = -1;
        public bool isPasswordField { get; set; }
        public char maskChar { get; set; } = '*';
        public bool isDelayed { get; set; }
        public bool HasSelection() => cursorIndex != selectIndex;
        public void SelectAll() { }
        public void SelectNone() { }
        public void SelectRange(int cursorIndex, int selectIndex) { }
        public void MoveBackward() { }
        public void MoveForward() { }
        public void MoveWordBackward() { }
        public void MoveWordForward() { }
        public void MoveToEndOfPreviousWord() { }
        public void MoveToStartOfNextWord() { }
        public void MoveToParagraphStart() { }
        public void MoveToParagraphEnd() { }
        public void SetValueWithoutNotify(T value) { }
    }
    public abstract class TextValueField<T> : TextInputBaseField<T>, IValueField<T>
    {
        public string formatString { get; set; } = "";
        public bool supportExpressions { get; set; }
        public void ClearValue() { }
        public T ApplyInputDeviceDelta(UnityEngine.Vector3 delta, DeltaSpeed speed,
            T startValue) => startValue;
        public void StartDragging() { }
        public void StopDragging() { }
    }
    public class TextField : TextInputBaseField<string>
    {
        public TextField() { }
        public TextField(string label) { }
        public TextField(int maxLength, bool multiline = false,
            bool isPasswordField = false, char maskChar = '*') { }
        public TextField(string label, int maxLength, bool multiline = false,
            bool isPasswordField = false, char maskChar = '*') { }
        public bool multiline { get; set; }
    }
    public class Toggle : BaseBoolField
    {
        public Toggle() { }
        public Toggle(string label) { this.label = label; }
        public string label { get; set; } = "";
        public bool value { get; set; }
        public void SetValueWithoutNotify(bool value) { }
    }
    public class RadioButton : Toggle
    {
        public RadioButton() { }
        public RadioButton(string label) { this.label = label; }
        public void SetSelected(bool selected) { }
    }
    public class RadioButtonGroup : BaseField<int>
    {
        public RadioButtonGroup() { }
        public RadioButtonGroup(string label, System.Collections.Generic.List<string> choices) { }
        public string label { get; set; } = "";
        public System.Collections.Generic.List<string> choices { get; set; } = new();
        public int value { get; set; } = -1;
        public void SetValueWithoutNotify(int value) { }
    }
    public struct ToggleButtonGroupState
    {
        public ToggleButtonGroupState(ulong optionsBitMask, int length) { this = default; }
        public int length => 0;
        public bool this[int index] { get => false; set { } }
        public void ResetAllOptions() { }
        public void SetAllOptions() { }
        public void ToggleAllOptions() { }
        public System.Span<int> GetActiveOptions(System.Span<int> destination) => default;
        public System.Span<int> GetInactiveOptions(System.Span<int> destination) => default;
        public static ToggleButtonGroupState CreateFromOptions(
            System.Collections.Generic.IList<bool> options) => default;
        public static ToggleButtonGroupState FromEnumFlags<T>(T options, int length)
            where T : System.Enum => default;
        public static T ToEnumFlags<T>(ToggleButtonGroupState options,
            bool acceptsLengthMismatch) where T : System.Enum => default!;
        public static bool Compare<T>(ToggleButtonGroupState options, T value)
            where T : System.Enum => false;
        public bool Equals(ToggleButtonGroupState other) => false;
        public int CompareTo(ToggleButtonGroupState other) => 0;
        public override bool Equals(object? other) => false;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
        public static bool operator ==(ToggleButtonGroupState left,
            ToggleButtonGroupState right) => false;
        public static bool operator !=(ToggleButtonGroupState left,
            ToggleButtonGroupState right) => true;
    }
    public class ToggleButtonGroup : BaseField<ToggleButtonGroupState>
    {
        public static string buttonClassName { get; }
        public static string buttonGroupClassName { get; }
        public static string buttonLeftClassName { get; }
        public static string buttonMidClassName { get; }
        public static string buttonRightClassName { get; }
        public static string buttonStandaloneClassName { get; }
        public static string emptyStateLabelClassName { get; }
        public ToggleButtonGroup() { }
        public ToggleButtonGroup(string label) { this.label = label; }
        public ToggleButtonGroup(ToggleButtonGroupState state) { value = state; }
        public ToggleButtonGroup(string label, ToggleButtonGroupState state)
            { this.label = label; value = state; }
        public string label { get; set; } = "";
        public bool allowEmptySelection { get; set; }
        public bool isMultipleSelection { get; set; }
        public ToggleButtonGroupState value { get; set; }
        public void SetValueWithoutNotify(ToggleButtonGroupState value) { }
        public Button GetButton(int index) => null;
    }
    public enum TwoPaneSplitViewOrientation { Horizontal = 0, Vertical = 1 }
    public class TwoPaneSplitView : VisualElement
    {
        public TwoPaneSplitView() { }
        public TwoPaneSplitView(int fixedPaneIndex, float fixedPaneStartDimension,
            TwoPaneSplitViewOrientation orientation) { }
        public int fixedPaneIndex { get; set; }
        public float fixedPaneInitialDimension { get; set; }
        public TwoPaneSplitViewOrientation orientation { get; set; }
        public VisualElement fixedPane { get; }
        public VisualElement flexedPane { get; }
        public void CollapseChild(int index) { }
        public void UnCollapse() { }
    }
    public class DropdownMenuItem { }
    public class DropdownMenuSeparator : DropdownMenuItem
    {
        public DropdownMenuSeparator(string subMenuPath) { }
        public string subMenuPath { get; }
    }
    public partial class DropdownMenuAction : DropdownMenuItem
    {
        public enum Status { None = 0, Normal = 1, Disabled = 2, Checked = 4, Hidden = 8 }
        public DropdownMenuAction(string name, System.Action<DropdownMenuAction> action,
            System.Func<DropdownMenuAction, Status> statusCallback, object userData) { }
        public string name { get; }
        public object userData { get; }
        public string tooltip { get; set; } = "";
        public Status status { get; }
        public DropdownMenuEventInfo eventInfo { get; }
        public static Status AlwaysEnabled(DropdownMenuAction action) => Status.Normal;
        public static Status AlwaysDisabled(DropdownMenuAction action) => Status.Disabled;
        public void Execute() { }
        public void UpdateActionStatus(DropdownMenuEventInfo eventInfo) { }
    }
    public class DropdownMenuEventInfo
    {
        public DropdownMenuEventInfo(EventBase evt) { }
        public UnityEngine.Vector2 mousePosition { get; }
        public UnityEngine.Vector2 localMousePosition { get; }
        public UnityEngine.EventModifiers modifiers { get; }
    }
    public class DropdownMenu
    {
        public bool allowDuplicateNames { get; set; }
        public void AppendAction(string name, System.Action<DropdownMenuAction> action,
            DropdownMenuAction.Status status = DropdownMenuAction.Status.Normal,
            object userData = null!) { }
        public void AppendAction(string name, System.Action<DropdownMenuAction> action,
            System.Func<DropdownMenuAction, DropdownMenuAction.Status> status,
            object userData = null!) { }
        public void AppendSeparator(string path = "") { }
        public void InsertAction(int index, string name,
            System.Action<DropdownMenuAction> action, DropdownMenuAction.Status status) { }
        public void InsertAction(int index, string name,
            System.Action<DropdownMenuAction> action,
            System.Func<DropdownMenuAction, DropdownMenuAction.Status> status,
            object userData = null!) { }
        public void InsertSeparator(string path, int index) { }
        public void RemoveItemAt(int index) { }
        public void ClearItems() { }
        public System.Collections.Generic.IEnumerable<object> MenuItems()
            => System.Array.Empty<object>();
        public void PrepareForDisplay(EventBase evt = null!) { }
    }
    public abstract class ContextualMenuManager { }
    public class ContextualMenuPopulateEvent : MouseEventBase<ContextualMenuPopulateEvent>
    {
        public static ContextualMenuPopulateEvent GetPooled(EventBase triggerEvent,
            DropdownMenu menu, IEventHandler target, ContextualMenuManager manager) => default!;
        public DropdownMenu menu { get; }
        public EventBase triggerEvent { get; }
    }
    public class GenericDropdownMenu
    {
        public static string ussClassName => "unity-base-dropdown";
        public static string itemUssClassName => "unity-base-dropdown__item";
        public static string itemContentUssClassName => "unity-base-dropdown__item-content";
        public static string labelUssClassName => "unity-base-dropdown__label";
        public static string containerInnerUssClassName => "unity-base-dropdown__container-inner";
        public static string containerOuterUssClassName => "unity-base-dropdown__container-outer";
        public static string checkmarkUssClassName => "unity-base-dropdown__checkmark";
        public static string separatorUssClassName => "unity-base-dropdown__separator";
        public static string contentWidthUssClassName => "unity-base-dropdown--content-width-menu";
        public GenericDropdownMenu() { }
        public VisualElement contentContainer { get; }
        public VisualElement targetElement { get; }
        public event System.Action onOpen { add { } remove { } }
        public event System.Action onClose { add { } remove { } }
        public void AddItem(string itemName, bool isChecked, System.Action action) { }
        public void AddItem(string itemName, bool isChecked,
            System.Action<object> action, object data) { }
        public void AddDisabledItem(string itemName, bool isChecked) { }
        public void AddSeparator(string path) { }
        public void DropDown(UnityEngine.Rect position) { }
        public void DropDown(UnityEngine.Rect position, VisualElement targetElement,
            bool anchored) { }
        public void DropDown(UnityEngine.Rect position, VisualElement targetElement,
            DropdownMenuSizeMode sizeMode) { }
        public void DropDown(UnityEngine.Rect position, VisualElement targetElement,
            bool anchored, bool fitContentWidthIfAnchored) { }
    }
    public class Foldout : BindableElement
    {
        public string text { get; set; } = "";
        public bool toggleOnLabelClick { get; set; } = true;
        public bool value { get; set; } = true;
        public void SetValueWithoutNotify(bool value) { }
    }
    public class IntegerField : TextValueField<int>
    {
        public IntegerField() { }
        public IntegerField(int maxLength) { }
        public IntegerField(string label, int maxLength = -1) { }
    }
    public class FloatField : TextValueField<float>
    {
        public FloatField() { }
        public FloatField(int maxLength) { }
        public FloatField(string label, int maxLength = -1) { }
    }
    public class LongField : TextValueField<long>
    {
        public LongField() { }
        public LongField(int maxLength) { }
        public LongField(string label, int maxLength = -1) { }
    }
    public class UnsignedIntegerField : TextValueField<uint>
    {
        public UnsignedIntegerField() { }
        public UnsignedIntegerField(int maxLength) { }
        public UnsignedIntegerField(string label, int maxLength = -1) { }
    }
    public class UnsignedLongField : TextValueField<ulong>
    {
        public UnsignedLongField() { }
        public UnsignedLongField(int maxLength) { }
        public UnsignedLongField(string label, int maxLength = -1) { }
    }
    public class DoubleField : TextValueField<double>
    {
        public DoubleField() { }
        public DoubleField(int maxLength) { }
        public DoubleField(string label, int maxLength = -1) { }
    }
    public class Vector2Field : BaseCompositeField<UnityEngine.Vector2, FloatField, float>
    {
        public Vector2Field() { }
        public Vector2Field(string label) { }
        public string label { get; set; } = "";
        public UnityEngine.Vector2 value { get; set; }
        public void SetValueWithoutNotify(UnityEngine.Vector2 value) { }
    }
    public class Vector3Field : BaseCompositeField<UnityEngine.Vector3, FloatField, float>
    {
        public Vector3Field() { }
        public Vector3Field(string label) { }
        public string label { get; set; } = "";
        public UnityEngine.Vector3 value { get; set; }
        public void SetValueWithoutNotify(UnityEngine.Vector3 value) { }
    }
    public class Vector4Field : BaseCompositeField<UnityEngine.Vector4, FloatField, float>
    {
        public Vector4Field() { }
        public Vector4Field(string label) { }
        public string label { get; set; } = "";
        public UnityEngine.Vector4 value { get; set; }
        public void SetValueWithoutNotify(UnityEngine.Vector4 value) { }
    }
    public class Vector2IntField : BaseCompositeField<UnityEngine.Vector2Int, IntegerField, int>
    {
        public Vector2IntField() { }
        public Vector2IntField(string label) { this.label = label; }
        public string label { get; set; } = "";
        public UnityEngine.Vector2Int value { get; set; }
        public void SetValueWithoutNotify(UnityEngine.Vector2Int value) { }
    }
    public class Vector3IntField : BaseCompositeField<UnityEngine.Vector3Int, IntegerField, int>
    {
        public Vector3IntField() { }
        public Vector3IntField(string label) { this.label = label; }
        public string label { get; set; } = "";
        public UnityEngine.Vector3Int value { get; set; }
        public void SetValueWithoutNotify(UnityEngine.Vector3Int value) { }
    }
    public class RectField : BaseCompositeField<UnityEngine.Rect, FloatField, float>
    {
        public RectField() { }
        public RectField(string label) { this.label = label; }
        public string label { get; set; } = "";
        public UnityEngine.Rect value { get; set; }
        public void SetValueWithoutNotify(UnityEngine.Rect value) { }
    }
    public class RectIntField : BaseCompositeField<UnityEngine.RectInt, IntegerField, int>
    {
        public RectIntField() { }
        public RectIntField(string label) { this.label = label; }
        public string label { get; set; } = "";
        public UnityEngine.RectInt value { get; set; }
        public void SetValueWithoutNotify(UnityEngine.RectInt value) { }
    }
    public class BoundsField : BaseCompositeField<UnityEngine.Bounds, FloatField, float>
    {
        public BoundsField() { }
        public BoundsField(string label) { this.label = label; }
        public string label { get; set; } = "";
        public UnityEngine.Bounds value { get; set; }
        public void SetValueWithoutNotify(UnityEngine.Bounds value) { }
    }
    public class BoundsIntField : BaseCompositeField<UnityEngine.BoundsInt, IntegerField, int>
    {
        public BoundsIntField() { }
        public BoundsIntField(string label) { this.label = label; }
        public string label { get; set; } = "";
        public UnityEngine.BoundsInt value { get; set; }
        public void SetValueWithoutNotify(UnityEngine.BoundsInt value) { }
    }
    public class MinMaxSlider : BaseField<UnityEngine.Vector2>
    {
        public MinMaxSlider() { }
        public MinMaxSlider(float minValue, float maxValue, float minLimit, float maxLimit) { }
        public MinMaxSlider(string label, float minValue, float maxValue,
            float minLimit, float maxLimit) { }
        public string label { get; set; } = "";
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public float lowLimit { get; set; }
        public float highLimit { get; set; }
        public float range { get; }
        public UnityEngine.Vector2 value { get; set; }
        public void SetValueWithoutNotify(UnityEngine.Vector2 value) { }
    }
    public class BaseSlider<T> : BaseField<T>, IValueField<T>
    {
        public static string dragContainerUssClassName => "";
        public static string draggerBorderUssClassName => "";
        public static string draggerUssClassName => "";
        public static string fillUssClassName => "";
        public static string horizontalVariantUssClassName => "";
        public static string inputUssClassName => "";
        public static string labelUssClassName => "";
        public static string movableUssClassName => "";
        public static string textFieldClassName => "";
        public static string trackerUssClassName => "";
        public static string ussClassName => "";
        public static string verticalVariantUssClassName => "";
        public SliderDirection direction { get; set; }
        public bool fill { get; set; }
        public bool inverted { get; set; }
        public T lowValue { get; set; } = default!;
        public T highValue { get; set; } = default!;
        public float pageSize { get; set; }
        public T range { get; } = default!;
        public bool showInputField { get; set; }
        public T value { get; set; } = default!;
        public void SetValueWithoutNotify(T value) { }
        public void AdjustDragElement(float factor) { }
        public T ApplyInputDeviceDelta(UnityEngine.Vector3 delta, DeltaSpeed speed,
            T startValue) => startValue;
        public void StartDragging() { }
        public void StopDragging() { }
    }
    public class Slider : BaseSlider<float>
    {
        public Slider() { }
        public Slider(float lowValue, float highValue) { }
        public Slider(string label, float lowValue, float highValue) { }
        public Slider(float lowValue, float highValue, SliderDirection direction,
            float pageSize) { }
        public Slider(string label, float lowValue, float highValue,
            SliderDirection direction, float pageSize) { }
        public string label { get; set; } = "";
    }
    public class SliderInt : BaseSlider<int>
    {
        public SliderInt() { }
        public SliderInt(int lowValue, int highValue) { }
        public SliderInt(string label, int lowValue, int highValue) { }
        public SliderInt(int lowValue, int highValue, SliderDirection direction,
            float pageSize) { }
        public SliderInt(string label, int lowValue, int highValue,
            SliderDirection direction, float pageSize) { }
        public string label { get; set; } = "";
    }
    public class BasePopupField<TValueType, TValueChoice> : BaseField<TValueType>
    {
        public string label { get; set; } = "";
        public System.Collections.Generic.List<TValueChoice> choices { get; set; } = new();
        public TValueType value { get; set; } = default!;
        public string text { get; }
        public void SetValueWithoutNotify(TValueType value) { }
    }
    public class PopupField<T> : BasePopupField<T, T>
    {
        public PopupField() { }
        public PopupField(string label) { this.label = label; }
        public PopupField(System.Collections.Generic.List<T> choices, int defaultIndex,
            System.Func<T, string>? formatSelectedValueCallback = null,
            System.Func<T, string>? formatListItemCallback = null) { }
        public PopupField(System.Collections.Generic.List<T> choices, T defaultValue,
            System.Func<T, string>? formatSelectedValueCallback = null,
            System.Func<T, string>? formatListItemCallback = null) { }
        public PopupField(string label, System.Collections.Generic.List<T> choices, int defaultIndex,
            System.Func<T, string>? formatSelectedValueCallback = null,
            System.Func<T, string>? formatListItemCallback = null) { }
        public PopupField(string label, System.Collections.Generic.List<T> choices, T defaultValue,
            System.Func<T, string>? formatSelectedValueCallback = null,
            System.Func<T, string>? formatListItemCallback = null) { }
        public System.Func<T, string>? formatListItemCallback { get; set; }
        public System.Func<T, string>? formatSelectedValueCallback { get; set; }
        public int index { get; set; }
    }
    public class DropdownField : PopupField<string>
    {
        public DropdownField() { }
        public DropdownField(string label) { }
        public DropdownField(System.Collections.Generic.List<string> choices, int defaultIndex,
            System.Func<string, string>? formatSelectedValueCallback = null,
            System.Func<string, string>? formatListItemCallback = null) { }
        public DropdownField(System.Collections.Generic.List<string> choices, string defaultValue,
            System.Func<string, string>? formatSelectedValueCallback = null,
            System.Func<string, string>? formatListItemCallback = null) { }
        public DropdownField(string label, System.Collections.Generic.List<string> choices, int defaultIndex,
            System.Func<string, string>? formatSelectedValueCallback = null,
            System.Func<string, string>? formatListItemCallback = null) { }
        public DropdownField(string label, System.Collections.Generic.List<string> choices, string defaultValue,
            System.Func<string, string>? formatSelectedValueCallback = null,
            System.Func<string, string>? formatListItemCallback = null) { }
    }
    public abstract class AbstractProgressBar : VisualElement
    {
        protected AbstractProgressBar() { }
        public string title { get; set; } = "";
        public float lowValue { get; set; }
        public float highValue { get; set; } = 100;
        public float value { get; set; }
        public void SetValueWithoutNotify(float value) { }
    }
    public class ProgressBar : AbstractProgressBar { public ProgressBar() { } }
    public partial class ScrollView : VisualElement
    {
        public static string ussClassName => "";
        public static string viewportUssClassName => "";
        public static string contentUssClassName => "";
        public static string contentAndVerticalScrollUssClassName => "";
        public static string hScrollerUssClassName => "";
        public static string vScrollerUssClassName => "";
        public static string horizontalVariantUssClassName => "";
        public static string verticalVariantUssClassName => "";
        public static string verticalHorizontalVariantUssClassName => "";
        public static string scrollVariantUssClassName => "";
        public static string horizontalVariantViewportUssClassName => "";
        public static string verticalVariantViewportUssClassName => "";
        public static string verticalHorizontalVariantViewportUssClassName => "";
        public static string horizontalVariantContentUssClassName => "";
        public static string verticalVariantContentUssClassName => "";
        public static string verticalHorizontalVariantContentUssClassName => "";
        public ScrollView() { }
        public ScrollView(ScrollViewMode mode) { }
        public ScrollViewMode mode { get; set; }
        public UnityEngine.Vector2 scrollOffset { get; set; }
        public Scroller horizontalScroller { get; }
        public Scroller verticalScroller { get; }
        public ScrollerVisibility horizontalScrollerVisibility { get; set; }
        public ScrollerVisibility verticalScrollerVisibility { get; set; }
        public float horizontalPageSize { get; set; }
        public float verticalPageSize { get; set; }
        public long elasticAnimationIntervalMs { get; set; }
        public float elasticity { get; set; }
        public float mouseWheelScrollSize { get; set; }
        public float scrollDecelerationRate { get; set; }
        public NestedInteractionKind nestedInteractionKind { get; set; }
        public TouchScrollBehavior touchScrollBehavior { get; set; }
        public bool showHorizontal { get; set; }
        public bool showVertical { get; set; }
        public VisualElement contentContainer { get; }
        public VisualElement contentViewport { get; }
        public void ScrollTo(VisualElement child) { }
    }
    public class Scroller : VisualElement
    {
        public Scroller() { }
        public Scroller(float lowValue, float highValue,
            System.Action<float> valueChanged, SliderDirection direction) { }
        public float lowValue { get; set; }
        public float highValue { get; set; }
        public float value { get; set; }
        public SliderDirection direction { get; set; }
        public Slider slider { get; }
        public Button lowButton { get; }
        public Button highButton { get; }
        public event System.Action<float> valueChanged { add { } remove { } }
        public void Adjust(float factor) { }
        public void ScrollPageUp() { }
        public void ScrollPageUp(float factor) { }
        public void ScrollPageDown() { }
        public void ScrollPageDown(float factor) { }
    }
    public class Tab : VisualElement
    {
        public Tab() { }
        public Tab(string label) { }
        public Tab(Background background) { }
        public Tab(string label, Background background) { }
        public static string ussClassName => "unity-tab";
        public static string tabHeaderUssClassName => "unity-tab__header";
        public static string tabHeaderImageUssClassName => "unity-tab__header-image";
        public static string tabHeaderEmptyImageUssClassName => "unity-tab__header-image--empty";
        public static string tabHeaderStandaloneImageUssClassName => "unity-tab__header-image--standalone";
        public static string tabHeaderLabelUssClassName => "unity-tab__header-label";
        public static string tabHeaderEmptyLabeUssClassName => "unity-tab__header-label--empty";
        public static string tabHeaderUnderlineUssClassName => "unity-tab__header-underline";
        public static string contentUssClassName => "unity-tab__content-container";
        public static string draggingUssClassName => "unity-tab--dragging";
        public static string reorderableUssClassName => "unity-tab__reorderable";
        public static string reorderableItemHandleUssClassName => "unity-tab__reorderable-handle";
        public static string reorderableItemHandleBarUssClassName => "unity-tab__reorderable-handle-bar";
        public static string closeableUssClassName => "unity-tab__header__closeable";
        public static string closeButtonUssClassName => "unity-tab__close-button";
        public string label { get; set; } = "";
        public bool closeable { get; set; }
        public VisualElement tabHeader { get; }
        public event System.Action<Tab> selected { add { } remove { } }
        public event System.Action<Tab> closed { add { } remove { } }
        public event System.Func<bool> closing { add { } remove { } }
    }
    public class TabView : VisualElement
    {
        public static string headerContainerClassName => "";
        public VisualElement contentViewport { get; }
        public bool reorderable { get; set; }
        public Tab activeTab { get; set; } = null!;
        public int selectedTabIndex { get; set; }
        public event System.Action<Tab, Tab> activeTabChanged { add { } remove { } }
        public event System.Action<Tab, int> tabClosed { add { } remove { } }
        public event System.Action<int, int> tabReordered { add { } remove { } }
        public Tab GetTab(int index) => null!;
        public VisualElement GetTabHeader(int index) => null!;
        public void ReorderTab(int from, int to) { }
    }
    public enum SelectionType { None = 0, Single = 1, Multiple = 2 }
    public enum CollectionVirtualizationMethod { FixedHeight = 0, DynamicHeight = 1 }
    public enum AlternatingRowBackground { None = 0, ContentOnly = 1, All = 2 }
    public enum ListViewReorderMode { Simple = 0, Animated = 1 }
    public enum BindingSourceSelectionMode { Manual = 0, AutoAssign = 1 }
    public class CollectionViewController { }
    public enum DragVisualMode { None = 0, Copy = 1, Move = 2, Rejected = 3, Link = 4 }
    public enum DragAndDropPosition { OverItem = 0, BetweenItems = 1, OutsideItems = 2 }
    public struct CanStartDragArgs
    {
        public VisualElement draggedElement { get; }
        public int id { get; }
        public System.Collections.Generic.IEnumerable<int> selectedIds { get; }
    }
    public struct StartDragArgs
    {
        public StartDragArgs(string title, DragVisualMode visualMode) { }
        public string title { get; }
        public DragVisualMode visualMode { get; }
        public void SetGenericData(string key, object value) { }
        public void SetPaths(string[] paths) { }
        public void SetUnityObjectReferences(System.Collections.Generic.IEnumerable<UnityEngine.Object> references) { }
        public void SetEntityIds(System.Collections.Generic.IReadOnlyList<UnityEngine.EntityId> ids) { }
    }
    public struct SetupDragAndDropArgs
    {
        public VisualElement draggedElement { get; }
        public System.Collections.Generic.IEnumerable<int> selectedIds { get; }
        public StartDragArgs startDragArgs { get; }
    }
    public class DragAndDropData
    {
        public object GetGenericData(string key) => null!;
        public void SetGenericData(string key, object value) { }
        public object source { get; }
        public string[] paths { get; }
        public System.Collections.Generic.IEnumerable<UnityEngine.Object> unityObjectReferences { get; }
        public System.Collections.Generic.IReadOnlyList<UnityEngine.EntityId> entityIds { get; }
        public DragVisualMode visualMode { get; }
    }
    public struct HandleDragAndDropArgs
    {
        public UnityEngine.Vector2 position { get; }
        public object target { get; }
        public int insertAtIndex { get; }
        public int parentId { get; }
        public int childIndex { get; }
        public DragAndDropPosition dropPosition { get; }
        public DragAndDropData dragAndDropData { get; }
    }
    public class BaseVerticalCollectionView : VisualElement
    {
        public static string ussClassName => "";
        public static string borderUssClassName => "";
        public static string dragHoverBarUssClassName => "";
        public static string dragHoverMarkerUssClassName => "";
        public static string itemAlternativeBackgroundUssClassName => "";
        public static string itemDragHoverUssClassName => "";
        public static string itemSelectedVariantUssClassName => "";
        public static string itemUssClassName => "";
        public static string listScrollViewUssClassName => "";
        public BaseVerticalCollectionView() { }
        public BaseVerticalCollectionView(System.Collections.IList itemsSource, float itemHeight) { }
        public BaseVerticalCollectionView(System.Collections.IList itemsSource, float itemHeight,
            System.Func<VisualElement> makeItem, System.Action<VisualElement, int> bindItem) { }
        public System.Collections.IList itemsSource { get; set; } = null!;
        public System.Func<VisualElement> makeItem { get; set; } = null!;
        public System.Action<VisualElement, int> bindItem { get; set; } = null!;
        public System.Action<VisualElement, int> unbindItem { get; set; } = null!;
        public float fixedItemHeight { get; set; } = 22;
        public bool reorderable { get; set; }
        public CollectionVirtualizationMethod virtualizationMethod { get; set; }
        public SelectionType selectionType { get; set; } = SelectionType.Single;
        public int selectedIndex { get; set; } = -1;
        public object selectedItem { get; }
        public System.Collections.Generic.IEnumerable<int> selectedIndices { get; }
        public System.Collections.Generic.IEnumerable<object> selectedItems { get; }
        public System.Collections.Generic.IEnumerable<int> selectedIds { get; }
        public float itemHeight { get; set; }
        public float resolvedItemHeight { get; }
        public bool horizontalScrollingEnabled { get; set; }
        public AlternatingRowBackground showAlternatingRowBackgrounds { get; set; }
        public bool showBorder { get; set; }
        public CollectionViewController viewController { get; }
        public void SetViewController(CollectionViewController controller) { }
        public event System.Action<System.Collections.Generic.IEnumerable<object>> selectionChanged { add { } remove { } }
        public event System.Action<System.Collections.Generic.IEnumerable<int>> selectedIndicesChanged { add { } remove { } }
        public event System.Action<System.Collections.Generic.IEnumerable<object>> itemsChosen { add { } remove { } }
        public event System.Action<System.Collections.Generic.IEnumerable<object>> onItemsChosen { add { } remove { } }
        public event System.Action<System.Collections.Generic.IEnumerable<object>> onSelectionChange { add { } remove { } }
        public event System.Action<System.Collections.Generic.IEnumerable<int>> onSelectedIndicesChange { add { } remove { } }
        public event System.Action itemsSourceChanged { add { } remove { } }
        public event System.Action<int, int> itemIndexChanged { add { } remove { } }
        public event System.Func<CanStartDragArgs, bool> canStartDrag { add { } remove { } }
        public event System.Func<SetupDragAndDropArgs, StartDragArgs> setupDragAndDrop { add { } remove { } }
        public event System.Func<HandleDragAndDropArgs, DragVisualMode> dragAndDropUpdate { add { } remove { } }
        public event System.Func<HandleDragAndDropArgs, DragVisualMode> handleDrop { add { } remove { } }
        public void SetSelection(int index) { }
        public void SetSelection(System.Collections.Generic.IEnumerable<int> indices) { }
        public void SetSelectionWithoutNotify(System.Collections.Generic.IEnumerable<int> indices) { }
        public void AddToSelection(int index) { }
        public void RemoveFromSelection(int index) { }
        public void ClearSelection() { }
        public void RefreshItems() { }
        public void Refresh() { }
        public void RefreshItem(int index) { }
        public void Rebuild() { }
        public void ScrollToItem(int index) { }
        public void ScrollToItemById(int id) { }
        public void ScrollToId(int id) { }
        public void ScrollTo(VisualElement element) { }
        public VisualElement GetRootElementForIndex(int index) => null!;
        public VisualElement GetRootElementForId(int id) => null!;
        public void OnKeyDown(KeyDownEvent evt) { }
    }
    public class BaseListView : BaseVerticalCollectionView
    {
        public new static string ussClassName => "";
        public static string arraySizeFieldUssClassName => "";
        public static string arraySizeFieldWithFooterUssClassName => "";
        public static string arraySizeFieldWithHeaderUssClassName => "";
        public static string emptyLabelUssClassName => "";
        public static string foldoutHeaderUssClassName => "";
        public static string footerAddButtonName => "";
        public static string footerRemoveButtonName => "";
        public static string footerUssClassName => "";
        public new static string itemUssClassName => "";
        public static string listViewWithFooterUssClassName => "";
        public static string listViewWithHeaderUssClassName => "";
        public static string overMaxMultiEditLimitClassName => "";
        public static string reorderableItemContainerUssClassName => "";
        public static string reorderableItemHandleBarUssClassName => "";
        public static string reorderableItemHandleUssClassName => "";
        public static string reorderableItemUssClassName => "";
        public static string reorderableUssClassName => "";
        public static string scrollViewWithFooterUssClassName => "";
        public BaseListView() { }
        public BaseListView(System.Collections.IList itemsSource, float itemHeight)
            : base(itemsSource, itemHeight) { }
        public bool allowAdd { get; set; } = true;
        public bool allowRemove { get; set; } = true;
        public bool showAddRemoveFooter { get; set; }
        public bool showFoldoutHeader { get; set; }
        public bool showBoundCollectionSize { get; set; }
        public string headerTitle { get; set; } = "";
        public System.Func<VisualElement> makeHeader { get; set; } = null!;
        public System.Func<VisualElement> makeFooter { get; set; } = null!;
        public System.Func<VisualElement> makeNoneElement { get; set; } = null!;
        public System.Action<BaseListView> onAdd { get; set; } = null!;
        public System.Action<BaseListView> onRemove { get; set; } = null!;
        public ListViewReorderMode reorderMode { get; set; }
        public BindingSourceSelectionMode bindingSourceSelectionMode { get; set; }
        public System.Action<BaseListView, Button> overridingAddButtonBehavior { get; set; } = null!;
        public event System.Action<System.Collections.Generic.IEnumerable<int>> itemsAdded { add { } remove { } }
        public event System.Action<System.Collections.Generic.IEnumerable<int>> itemsRemoved { add { } remove { } }
    }
    public class ListView : BaseListView
    {
        public ListView() { }
        public ListView(System.Collections.IList itemsSource, float itemHeight,
            System.Func<VisualElement> makeItem, System.Action<VisualElement, int> bindItem) { }
        public VisualTreeAsset itemTemplate { get; set; } = null!;
        public System.Action<VisualElement> destroyItem { get; set; } = null!;
    }
    public class Column
    {
        public string name { get; set; } = "";
        public string title { get; set; } = "";
        public string bindingPath { get; set; } = "";
        public float width { get; set; }
        public bool visible { get; set; } = true;
        public bool resizable { get; set; } = true;
        public bool sortable { get; set; } = true;
        public System.Func<VisualElement> makeCell { get; set; } = null!;
        public System.Action<VisualElement, int> bindCell { get; set; } = null!;
        public System.Action<VisualElement, int> unbindCell { get; set; } = null!;
        public System.Action<VisualElement> destroyCell { get; set; } = null!;
        public System.Func<VisualElement> makeHeader { get; set; } = null!;
        public System.Action<VisualElement> bindHeader { get; set; } = null!;
        public System.Action<VisualElement> unbindHeader { get; set; } = null!;
        public System.Action<VisualElement> destroyHeader { get; set; } = null!;
        public System.Comparison<int> comparison { get; set; } = null!;
        public float minWidth { get; set; }
        public float maxWidth { get; set; }
        public Columns collection { get; }
        public VisualTreeAsset cellTemplate { get; set; } = null!;
        public VisualTreeAsset headerTemplate { get; set; } = null!;
        public Background icon { get; set; }
        public bool optional { get; set; }
        public bool stretchable { get; set; }
    }
    public enum ColumnSortingMode { None = 0, Default = 1, Custom = 2 }
    public enum SortDirection { Ascending = 0, Descending = 1 }
    public class SortColumnDescription
    {
        public SortColumnDescription() { }
        public SortColumnDescription(int columnIndex, SortDirection direction) { }
        public SortColumnDescription(string columnName, SortDirection direction) { }
        public string columnName { get; set; } = "";
        public int columnIndex { get; set; } = -1;
        public SortDirection direction { get; set; }
        public Column column { get; }
    }
    public class SortColumnDescriptions : System.Collections.Generic.IEnumerable<SortColumnDescription>
    {
        public int Count { get; }
        public bool IsReadOnly { get; }
        public SortColumnDescription this[int index] { get => null!; set { } }
        public void Add(SortColumnDescription description) { }
        public void Insert(int index, SortColumnDescription description) { }
        public void Clear() { }
        public bool Contains(SortColumnDescription description) => false;
        public void CopyTo(SortColumnDescription[] target, int index) { }
        public int IndexOf(SortColumnDescription description) => -1;
        public bool Remove(SortColumnDescription description) => false;
        public void RemoveAt(int index) { }
        public System.Collections.Generic.IEnumerator<SortColumnDescription> GetEnumerator() => null!;
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public partial class Columns : System.Collections.Generic.IEnumerable<Column>
    {
        public string primaryColumnName { get; set; } = "";
        public bool reorderable { get; set; } = true;
        public bool resizable { get; set; } = true;
        public bool resizePreview { get; set; }
        public StretchMode stretchMode { get; set; }
        public event System.Action propertyChanged { add { } remove { } }
        public bool IsReadOnly { get; }
        public int Count { get; }
        public Column this[int index] { get => null!; set { } }
        public Column this[string name] { get => null!; }
        public void Add(Column column) { }
        public void Insert(int index, Column column) { }
        public bool Remove(Column column) => false;
        public void RemoveAt(int index) { }
        public void Clear() { }
        public bool Contains(Column column) => false;
        public bool Contains(string name) => false;
        public void CopyTo(Column[] target, int index) { }
        public int IndexOf(Column column) => -1;
        public bool IsPrimary(Column column) => false;
        public void ReorderDisplay(int from, int to) { }
        public System.Collections.Generic.IEnumerator<Column> GetEnumerator() => null!;
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class MultiColumnListView : BaseListView
    {
        public MultiColumnListView() { }
        public MultiColumnListView(Columns columns) { }
        public Columns columns { get; } = new Columns();
        public bool sortingEnabled { get; set; }
        public ColumnSortingMode sortingMode { get; set; }
        public SortColumnDescriptions sortColumnDescriptions { get; } = new SortColumnDescriptions();
        public System.Collections.Generic.IEnumerable<SortColumnDescription> sortedColumns { get; }
        public event System.Action columnSortingChanged { add { } remove { } }
        public event System.Action<ContextualMenuPopulateEvent, Column>
            headerContextMenuPopulateEvent { add { } remove { } }
    }
    public struct TreeViewItemData<T>
    {
        public TreeViewItemData(int id, T data,
            System.Collections.Generic.List<TreeViewItemData<T>>? children = null) { }
        public int id { get; }
        public T data { get; }
        public System.Collections.Generic.IEnumerable<TreeViewItemData<T>> children { get; }
        public bool hasChildren { get; }
    }
    public class TreeViewExpansionChangedArgs
    {
        public int id { get; }
        public bool isExpanded { get; }
        public bool isAppliedToAllChildren { get; }
    }
    public class BaseTreeView : BaseVerticalCollectionView
    {
        public BaseTreeView() { }
        public BaseTreeView(int itemHeight) { }
        public new static string ussClassName => "";
        public new static string itemUssClassName => "";
        public static string itemContentContainerUssClassName => "";
        public static string itemIndentUssClassName => "";
        public static string itemIndentsContainerUssClassName => "";
        public static string itemToggleUssClassName => "";
        public bool autoExpand { get; set; }
        public event System.Action<TreeViewExpansionChangedArgs> itemExpandedChanged { add { } remove { } }
        public void SetRootItems<T>(System.Collections.Generic.IList<TreeViewItemData<T>> items) { }
        public void AddItem<T>(TreeViewItemData<T> item, int parentId = -1,
            int childIndex = -1, bool rebuildTree = true) { }
        public bool TryRemoveItem(int id, bool rebuildTree = true) => false;
        public System.Collections.Generic.IEnumerable<int> GetRootIds() => System.Array.Empty<int>();
        public int GetTreeCount() => 0;
        public int GetIdForIndex(int index) => -1;
        public T GetItemDataForId<T>(int id) => default!;
        public T GetItemDataForIndex<T>(int index) => default!;
        public int GetParentIdForIndex(int index) => -1;
        public System.Collections.Generic.IEnumerable<int> GetChildrenIdsForIndex(int index)
            => System.Array.Empty<int>();
        public bool IsExpanded(int id) => false;
        public void ExpandItem(int id, bool expandAllChildren = false, bool refresh = true) { }
        public void CollapseItem(int id, bool collapseAllChildren = false, bool refresh = true) { }
        public void ExpandRootItems() { }
        public void ExpandAll() { }
        public void CollapseAll() { }
        public void SetSelectionById(int id) { }
        public void SetSelectionById(System.Collections.Generic.IEnumerable<int> ids) { }
        public void SetSelectionByIdWithoutNotify(System.Collections.Generic.IEnumerable<int> ids) { }
        public void AddToSelectionById(int id) { }
        public void RemoveFromSelectionById(int id) { }
        public System.Collections.Generic.IEnumerable<T> GetSelectedItems<T>() => System.Array.Empty<T>();
    }
    public class TreeView : BaseTreeView
    {
        public TreeView() { }
        public TreeView(System.Func<VisualElement> makeItem,
            System.Action<VisualElement, int> bindItem) { }
        public TreeView(int itemHeight, System.Func<VisualElement> makeItem,
            System.Action<VisualElement, int> bindItem) { }
        public VisualTreeAsset itemTemplate { get; set; } = null!;
        public System.Action<VisualElement> destroyItem { get; set; } = null!;
    }
    public class MultiColumnTreeView : BaseTreeView
    {
        public MultiColumnTreeView() { }
        public MultiColumnTreeView(Columns columns) { }
        public Columns columns { get; } = new Columns();
        public bool sortingEnabled { get; set; }
        public ColumnSortingMode sortingMode { get; set; }
        public SortColumnDescriptions sortColumnDescriptions { get; } = new SortColumnDescriptions();
        public System.Collections.Generic.IEnumerable<SortColumnDescription> sortedColumns { get; }
        public event System.Action columnSortingChanged { add { } remove { } }
        public event System.Action<ContextualMenuPopulateEvent, Column>
            headerContextMenuPopulateEvent { add { } remove { } }
    }
    public static class PointerId
    {
        public const int maxPointers = 32, invalidPointerId = -1, mousePointerId = 0;
        public const int touchPointerIdBase = 1, touchPointerCount = 20;
        public const int penPointerIdBase = 21, penPointerCount = 2;
        public const int trackedPointerIdBase = 23, trackedPointerCount = 8;
    }
    public static class PointerType
    {
        public const string mouse = "mouse", touch = "touch", pen = "pen";
        public const string tracked = "tracked", unknown = "";
    }
    public class InputWrapper
    {
        public UnityEngine.Vector2 compositionCursorPos { get; set; }
        public string compositionString { get; }
        public UnityEngine.IMECompositionMode imeCompositionMode { get; set; }
        public UnityEngine.Vector2 mousePosition { get; }
        public bool mousePresent { get; }
        public UnityEngine.Vector2 mouseScrollDelta { get; }
        public int touchCount { get; }
        public bool touchSupported { get; }
        public float GetAxisRaw(string axisName) => 0;
        public bool GetButtonDown(string buttonName) => false;
        public bool GetMouseButton(int button) => false;
        public bool GetMouseButtonDown(int button) => false;
        public bool GetMouseButtonUp(int button) => false;
        public UnityEngine.Touch GetTouch(int index) => default;
    }
    public abstract class EventBase : System.IDisposable
    {
        public VisualElement target { get; }
        public VisualElement currentTarget { get; }
        public int propagationPhase { get; }
        public long eventTypeId { get; }
        public long timestamp { get; }
        public bool bubbles { get; }
        public bool tricklesDown { get; }
        public bool isPropagationStopped { get; }
        public bool isImmediatePropagationStopped { get; }
        public bool isDefaultPrevented { get; }
        public bool ignoreDisabledElements { get; set; }
        public bool dispatch { get; }
        public UnityEngine.Event imguiEvent { get; }
        public UnityEngine.Vector2 originalMousePosition { get; }
        public void PreventDefault() { }
        public void Dispose() { }
        public void StopPropagation() { }
        public void StopImmediatePropagation() { }
    }
    public abstract class EventBase<T> : EventBase where T : EventBase<T>
    {
        public static T GetPooled() => default;
        public static long TypeId() => 0;
    }
    public interface INavigationEvent
    {
        UnityEngine.EventModifiers modifiers { get; }
        bool shiftKey { get; }
        bool ctrlKey { get; }
        bool altKey { get; }
        bool commandKey { get; }
        bool actionKey { get; }
    }
    public abstract class NavigationEventBase<T> : EventBase<T>, INavigationEvent
        where T : NavigationEventBase<T>
    {
        public static T GetPooled(UnityEngine.EventModifiers modifiers) => default!;
        public UnityEngine.EventModifiers modifiers { get; }
        public bool shiftKey { get; }
        public bool ctrlKey { get; }
        public bool altKey { get; }
        public bool commandKey { get; }
        public bool actionKey { get; }
    }
    public class NavigationSubmitEvent : NavigationEventBase<NavigationSubmitEvent> { }
    public class NavigationCancelEvent : NavigationEventBase<NavigationCancelEvent> { }
    public class NavigationMoveEvent : NavigationEventBase<NavigationMoveEvent>
    {
        public enum Direction { None, Left, Up, Right, Down, Next, Previous }
        public static NavigationMoveEvent GetPooled(Direction direction,
            UnityEngine.EventModifiers modifiers) => default!;
        public static NavigationMoveEvent GetPooled(UnityEngine.Vector2 move,
            UnityEngine.EventModifiers modifiers) => default!;
        public Direction direction { get; }
        public UnityEngine.Vector2 move { get; }
    }
    public class GeometryChangedEvent : EventBase<GeometryChangedEvent>
    {
        public static GeometryChangedEvent GetPooled(UnityEngine.Rect oldRect,
            UnityEngine.Rect newRect) => default!;
        public UnityEngine.Rect oldRect { get; }
        public UnityEngine.Rect newRect { get; }
    }
    public class InputEvent : EventBase<InputEvent>
    {
        public static InputEvent GetPooled(string previousData, string newData) => default!;
        public string previousData { get; }
        public string newData { get; }
    }
    public class TooltipEvent : EventBase<TooltipEvent>
    {
        public string tooltip { get; set; }
        public UnityEngine.Rect rect { get; set; }
    }
    public class CustomStyleResolvedEvent : EventBase<CustomStyleResolvedEvent>
    {
        public ICustomStyle customStyle { get; }
    }
    public interface IPanelChangedEvent { }
    public abstract class PanelChangedEventBase<T> : EventBase<T>, IPanelChangedEvent
        where T : PanelChangedEventBase<T>
    {
        public static T GetPooled(IPanel originPanel, IPanel destinationPanel) => default!;
        public IPanel originPanel { get; }
        public IPanel destinationPanel { get; }
    }
    public class AttachToPanelEvent : PanelChangedEventBase<AttachToPanelEvent> { }
    public class DetachFromPanelEvent : PanelChangedEventBase<DetachFromPanelEvent> { }
    public interface ICommandEvent { string commandName { get; } }
    public abstract class CommandEventBase<T> : EventBase<T>, ICommandEvent
        where T : CommandEventBase<T>
    {
        public static T GetPooled(string commandName) => default!;
        public string commandName { get; }
    }
    public class ExecuteCommandEvent : CommandEventBase<ExecuteCommandEvent> { }
    public class ValidateCommandEvent : CommandEventBase<ValidateCommandEvent> { }
    public class ContextClickEvent : MouseEventBase<ContextClickEvent> { }
    public interface IDragAndDropEvent { }
    public abstract class DragAndDropEventBase<T> : MouseEventBase<T>, IDragAndDropEvent
        where T : DragAndDropEventBase<T> { }
    public class DragEnterEvent : DragAndDropEventBase<DragEnterEvent> { }
    public class DragLeaveEvent : DragAndDropEventBase<DragLeaveEvent> { }
    public class DragUpdatedEvent : DragAndDropEventBase<DragUpdatedEvent> { }
    public class DragPerformEvent : DragAndDropEventBase<DragPerformEvent> { }
    public class DragExitedEvent : DragAndDropEventBase<DragExitedEvent> { }
    public interface ITransitionEvent
    {
        float elapsedTime { get; }
        System.Collections.Generic.IEnumerable<StylePropertyName> stylePropertyNames { get; }
    }
    public abstract class TransitionEventBase<T> : EventBase<T>, ITransitionEvent
        where T : TransitionEventBase<T>
    {
        public static T GetPooled(StylePropertyName property, double elapsedTime) => default!;
        public float elapsedTime { get; }
        public System.Collections.Generic.IEnumerable<StylePropertyName> stylePropertyNames { get; }
        public bool AffectsProperty(StylePropertyName property) => false;
    }
    public class TransitionRunEvent : TransitionEventBase<TransitionRunEvent> { }
    public class TransitionStartEvent : TransitionEventBase<TransitionStartEvent> { }
    public class TransitionEndEvent : TransitionEventBase<TransitionEndEvent> { }
    public class TransitionCancelEvent : TransitionEventBase<TransitionCancelEvent> { }
    public interface IPointerEvent
    {
        int pointerId { get; }
        public string pointerType { get; }
        public int button { get; }
        public int pressedButtons { get; }
        public UnityEngine.Vector3 position { get; }
        public UnityEngine.Vector3 localPosition { get; }
        public UnityEngine.Vector3 deltaPosition { get; }
        public float deltaTime { get; }
        public int clickCount { get; }
        public bool isPrimary { get; }
        public UnityEngine.EventModifiers modifiers { get; }
        public bool shiftKey { get; }
        public bool ctrlKey { get; }
        public bool altKey { get; }
        public bool commandKey { get; }
        public bool actionKey { get; }
        public float pressure { get; }
        public float tangentialPressure { get; }
        public float altitudeAngle { get; }
        public float azimuthAngle { get; }
        public float twist { get; }
        public UnityEngine.Vector2 tilt { get; }
        public UnityEngine.Vector2 radius { get; }
        public UnityEngine.Vector2 radiusVariance { get; }
        public int penStatus { get; }
    }
    public abstract class PointerEventBase<T> : EventBase<T>, IPointerEvent where T : EventBase<T>
    {
        public static T GetPooled(IPointerEvent source) => default!;
        public static T GetPooled(UnityEngine.Event source) => default!;
        public static T GetPooled(UnityEngine.PenData source,
            UnityEngine.EventModifiers modifiers) => default!;
        public static T GetPooled(UnityEngine.Touch source,
            UnityEngine.EventModifiers modifiers) => default!;
        public int pointerId { get; }
        public string pointerType { get; }
        public int button { get; }
        public int pressedButtons { get; }
        public UnityEngine.Vector3 position { get; }
        public UnityEngine.Vector3 localPosition { get; }
        public UnityEngine.Vector3 deltaPosition { get; }
        public float deltaTime { get; }
        public int clickCount { get; }
        public bool isPrimary { get; }
        public UnityEngine.EventModifiers modifiers { get; }
        public bool shiftKey { get; }
        public bool ctrlKey { get; }
        public bool altKey { get; }
        public bool commandKey { get; }
        public bool actionKey { get; }
        public float pressure { get; }
        public float tangentialPressure { get; }
        public float altitudeAngle { get; }
        public float azimuthAngle { get; }
        public float twist { get; }
        public UnityEngine.Vector2 tilt { get; }
        public UnityEngine.Vector2 radius { get; }
        public UnityEngine.Vector2 radiusVariance { get; }
        public int penStatus { get; }
    }
    public class ClickEvent : PointerEventBase<ClickEvent> { }
    public class PointerDownEvent : PointerEventBase<PointerDownEvent> { }
    public class PointerUpEvent : PointerEventBase<PointerUpEvent> { }
    public class PointerMoveEvent : PointerEventBase<PointerMoveEvent> { }
    public class PointerCancelEvent : PointerEventBase<PointerCancelEvent> { }
    public class PointerOverEvent : PointerEventBase<PointerOverEvent> { }
    public class PointerOutEvent : PointerEventBase<PointerOutEvent> { }
    public class PointerEnterEvent : PointerEventBase<PointerEnterEvent> { }
    public class PointerLeaveEvent : PointerEventBase<PointerLeaveEvent> { }
    public class PointerStationaryEvent : PointerEventBase<PointerStationaryEvent> { }
    public interface IPointerCaptureEvent { }
    public abstract class PointerCaptureEventBase<T> : PointerEventBase<T>, IPointerCaptureEvent
        where T : EventBase<T>
    {
        public IEventHandler relatedTarget { get; }
        public static T GetPooled(IEventHandler target, IEventHandler relatedTarget,
            int pointerId) => default!;
    }
    public class PointerCaptureEvent : PointerCaptureEventBase<PointerCaptureEvent> { }
    public class PointerCaptureOutEvent : PointerCaptureEventBase<PointerCaptureOutEvent> { }
    public class WheelEvent : ClickEvent
    {
        public const float scrollDeltaPerTick = 3f;
        public UnityEngine.Vector3 delta { get; }
    }
    public interface IMouseEvent
    {
        UnityEngine.Vector2 mousePosition { get; }
        UnityEngine.Vector2 localMousePosition { get; }
        UnityEngine.Vector2 mouseDelta { get; }
        int button { get; }
        int pressedButtons { get; }
        int clickCount { get; }
        UnityEngine.EventModifiers modifiers { get; }
        bool shiftKey { get; }
        bool ctrlKey { get; }
        bool altKey { get; }
        bool commandKey { get; }
        bool actionKey { get; }
    }
    public abstract class MouseEventBase<T> : EventBase<T>, IMouseEvent where T : MouseEventBase<T>
    {
        public static T GetPooled(IMouseEvent source) => default!;
        public static T GetPooled(UnityEngine.Event source) => default!;
        public static T GetPooled(UnityEngine.Vector2 mousePosition, int button,
            int clickCount, UnityEngine.Vector2 mouseDelta,
            UnityEngine.EventModifiers modifiers) => default!;
        public UnityEngine.Vector2 mousePosition { get; }
        public UnityEngine.Vector2 localMousePosition { get; }
        public UnityEngine.Vector2 mouseDelta { get; }
        public int button { get; }
        public int pressedButtons { get; }
        public int clickCount { get; }
        public UnityEngine.EventModifiers modifiers { get; }
        public bool shiftKey { get; }
        public bool ctrlKey { get; }
        public bool altKey { get; }
        public bool commandKey { get; }
        public bool actionKey { get; }
    }
    public class MouseDownEvent : MouseEventBase<MouseDownEvent> { }
    public class MouseUpEvent : MouseEventBase<MouseUpEvent> { }
    public class MouseMoveEvent : MouseEventBase<MouseMoveEvent> { }
    public class MouseOverEvent : MouseEventBase<MouseOverEvent> { }
    public class MouseOutEvent : MouseEventBase<MouseOutEvent> { }
    public class MouseEnterEvent : MouseEventBase<MouseEnterEvent> { }
    public class MouseLeaveEvent : MouseEventBase<MouseLeaveEvent> { }
    public class MouseEnterWindowEvent : MouseEventBase<MouseEnterWindowEvent> { }
    public class MouseLeaveWindowEvent : MouseEventBase<MouseLeaveWindowEvent> { }
    public interface IMouseCaptureEvent { }
    public abstract class MouseCaptureEventBase<T> : MouseEventBase<T>, IMouseCaptureEvent
        where T : MouseCaptureEventBase<T>
    {
        public IEventHandler relatedTarget { get; }
        public static T GetPooled(IEventHandler target, IEventHandler relatedTarget) => default!;
    }
    public class MouseCaptureEvent : MouseCaptureEventBase<MouseCaptureEvent> { }
    public class MouseCaptureOutEvent : MouseCaptureEventBase<MouseCaptureOutEvent> { }
    public interface IFocusEvent
    {
        Focusable relatedTarget { get; }
        FocusChangeDirection direction { get; }
    }
    public abstract class FocusEventBase<T> : EventBase<T>, IFocusEvent
        where T : EventBase<T>
    {
        public Focusable relatedTarget { get; }
        public FocusChangeDirection direction { get; }
        public static T GetPooled(IEventHandler target, Focusable relatedTarget,
            FocusChangeDirection direction, FocusController focusController,
            bool delegatedFocus) => default!;
    }
    public class FocusInEvent : FocusEventBase<FocusInEvent> { }
    public class FocusOutEvent : FocusEventBase<FocusOutEvent> { }
    public class FocusEvent : FocusEventBase<FocusEvent> { }
    public class BlurEvent : FocusEventBase<BlurEvent> { }
    public interface IKeyboardEvent
    {
        UnityEngine.KeyCode keyCode { get; }
        char character { get; }
        UnityEngine.EventModifiers modifiers { get; }
        bool shiftKey { get; }
        bool ctrlKey { get; }
        bool altKey { get; }
        bool commandKey { get; }
        bool actionKey { get; }
    }
    public abstract class KeyboardEventBase<T> : EventBase<T>, IKeyboardEvent
        where T : KeyboardEventBase<T>
    {
        public static T GetPooled(UnityEngine.Event source) => default!;
        public static T GetPooled(char character, UnityEngine.KeyCode keyCode,
            UnityEngine.EventModifiers modifiers) => default!;
        public UnityEngine.KeyCode keyCode { get; }
        public char character { get; }
        public UnityEngine.EventModifiers modifiers { get; }
        public bool shiftKey { get; }
        public bool ctrlKey { get; }
        public bool altKey { get; }
        public bool commandKey { get; }
        public bool actionKey { get; }
    }
    public class KeyDownEvent : KeyboardEventBase<KeyDownEvent> { }
    public class KeyUpEvent : KeyboardEventBase<KeyUpEvent> { }
    public interface IChangeEvent { }
    public class ChangeEvent<T> : EventBase<ChangeEvent<T>>, IChangeEvent
    {
        public static ChangeEvent<T> GetPooled(T previousValue, T newValue) => default;
        public T previousValue { get; }
        public T newValue { get; }
    }
    public class VisualTreeAsset : UnityEngine.ScriptableObject
    {
        public int contentHash { get; }
        public bool importedWithErrors { get; }
        public bool importedWithWarnings { get; }
        public System.Collections.Generic.IEnumerable<VisualTreeAsset> templateDependencies { get; }
        public TemplateContainer CloneTree() => null;
        public TemplateContainer CloneTree(string bindingPath) => null;
        public void CloneTree(VisualElement target) { }
        public void CloneTree(VisualElement target, out int firstElementIndex,
            out int elementAddedCount) { firstElementIndex = 0; elementAddedCount = 0; }
        public TemplateContainer Instantiate() => null;
        public TemplateContainer Instantiate(string bindingPath) => null;
    }
    public class PanelRaycaster : UnityEngine.EventSystems.BaseRaycaster
    {
        public object panel { get; set; }
        public override UnityEngine.Camera eventCamera => null;
        public override int sortOrderPriority => 0;
        public override int renderOrderPriority => 0;
        public override void Raycast(UnityEngine.EventSystems.PointerEventData eventData,
            System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> resultAppendList) { }
    }
    public class PanelEventHandler : UnityEngine.EventSystems.UIBehaviour
    {
        public object panel { get; set; }
    }
    public class WorldDocumentRaycaster : UnityEngine.EventSystems.BaseRaycaster
    {
        public UnityEngine.Camera camera { get; set; }
        public override UnityEngine.Camera eventCamera => camera;
    }
    public class PanelSettings : UnityEngine.ScriptableObject
    {
        public PanelScaleMode scaleMode { get; set; }
        public float scale { get; set; } = 1;
        public UnityEngine.Vector2Int referenceResolution { get; set; }
        public PanelScreenMatchMode screenMatchMode { get; set; }
        public float match { get; set; }
        public float sortingOrder { get; set; }
        public int targetDisplay { get; set; }
        public float fallbackDpi { get; set; }
        public float referenceDpi { get; set; }
        public float referenceSpritePixelsPerUnit { get; set; }
        public bool clearColor { get; set; }
        public bool clearDepthStencil { get; set; }
        public UnityEngine.Color colorClearValue { get; set; }
        public float depthClearValue { get; }
        public PanelRenderMode renderMode { get; set; }
        public UnityEngine.RenderTexture targetTexture { get; set; }
        public bool forceGammaRendering;
        public object dynamicAtlasSettings { get; set; }
        public TextureSlotCount textureSlotCount { get; set; }
        public uint vertexBudget { get; set; }
        public object textSettings;
        public ThemeStyleSheet themeStyleSheet { get; set; }
        public void SetScreenToPanelSpaceFunction(System.Func<UnityEngine.Vector2, UnityEngine.Vector2> callback) { }
        public void SetScreenToPanelSpaceFunction3D(System.Func<UnityEngine.Vector2, UnityEngine.Vector3> callback) { }
        public void SetPanelChangeReceiver(IDebugPanelChangeReceiver receiver) { }
    }
    public interface IDebugPanelChangeReceiver { }
    public partial class UIDocument : UnityEngine.MonoBehaviour
    {
        public UIDocument parentUI { get; }
        public UnityEngine.Vector2 pivot { get; set; }
        public UnityEngine.Vector2 pivotReferenceSize { get; set; }
        public UnityEngine.Vector3 position { get; set; }
        public UnityEngine.Vector2 worldSpaceSize { get; set; }
        public object worldSpaceSizeMode { get; set; }
        public PanelSettings panelSettings { get; set; }
        public VisualTreeAsset visualTreeAsset { get; set; }
        public float sortingOrder { get; set; }
        public VisualElement rootVisualElement { get; }
        public IRuntimePanel runtimePanel { get; }
    }
}
namespace TMPro
{
    [System.Flags]
    public enum FontStyles { Normal = 0, Bold = 1, Italic = 2, Underline = 4,
        LowerCase = 8, UpperCase = 16, SmallCaps = 32, Strikethrough = 64 }
    public enum TextAlignmentOptions { TopLeft = 257, Top = 258, TopRight = 260,
        Left = 513, Center = 514, Right = 516,
        BottomLeft = 1025, Bottom = 1026, BottomRight = 1028 }
    public enum TextOverflowModes { Overflow = 0, Ellipsis = 1, Masking = 2,
        Truncate = 3, ScrollRect = 4 }
    public enum TextWrappingModes { NoWrap = 0, Normal = 1 }
    public class TMP_FontAsset : UnityEngine.Object
    {
        public bool HasCharacter(char character) => false;
        public bool TryAddCharacters(string characters) => false;
        public bool TryAddCharacters(string characters, out string missingCharacters)
        { missingCharacters = characters; return false; }
        public void ClearFontAssetData() { }
    }
    public class TMP_SpriteAsset : UnityEngine.Object { }
    public class TMP_Text : UnityEngine.UI.MaskableGraphic
    {
        public string text { get; set; }
        public TMP_FontAsset font { get; set; }
        public float fontSize { get; set; }
        public FontStyles fontStyle { get; set; }
        public TextAlignmentOptions alignment { get; set; }
        public bool enableWordWrapping { get; set; }
        public TextWrappingModes textWrappingMode { get; set; }
        public TextOverflowModes overflowMode { get; set; }
        public bool richText { get; set; }
        public bool enableAutoSizing { get; set; }
        public float fontSizeMin { get; set; }
        public float fontSizeMax { get; set; }
        public UnityEngine.Vector4 margin { get; set; }
        public TMP_SpriteAsset spriteAsset { get; set; }
        public UnityEngine.Material fontSharedMaterial { get; set; }
        public UnityEngine.Color32 faceColor { get; set; }
        public UnityEngine.Color32 outlineColor { get; set; }
        public float outlineWidth { get; set; }
        public float characterSpacing { get; set; }
        public float lineSpacing { get; set; }
        public float preferredWidth { get; }
        public float preferredHeight { get; }
        public UnityEngine.Vector2 GetPreferredValues() => default;
        public UnityEngine.Vector2 GetPreferredValues(string value) => default;
        public void ForceMeshUpdate() { }
        public void ForceMeshUpdate(bool ignoreActiveState, bool forceTextReparsing = false) { }
        public void UpdateVertexData() { }
        public void ClearMesh() { }
        public void SetText(string value, float arg0) { }
        public void SetText(string value, float arg0, float arg1) { }
        public void SetText(string value, float arg0, float arg1, float arg2) { }
        public void SetText(string value) { }
    }
    public class TextMeshProUGUI : TMP_Text { }
    public class TextMeshPro : TMP_Text { }
    public class TMP_InputField : UnityEngine.UI.Selectable
    {
        public class OnChangeEvent : UnityEngine.Events.UnityEvent<string> { }
        public class SubmitEvent : UnityEngine.Events.UnityEvent<string> { }
        public string text { get; set; }
        public TMP_Text textComponent { get; set; }
        public UnityEngine.UI.Graphic placeholder { get; set; }
        public int characterLimit { get; set; }
        public OnChangeEvent onValueChanged { get; set; } = new();
        public SubmitEvent onSubmit { get; set; } = new();
        public SubmitEvent onEndEdit { get; set; } = new();
        public void SetTextWithoutNotify(string value) { }
        public void ActivateInputField() { }
        public void DeactivateInputField() { }
        public bool isFocused { get; }
        public int caretPosition { get; set; }
        public int selectionAnchorPosition { get; set; }
        public int selectionFocusPosition { get; set; }
    }
    public class TMP_Dropdown : UnityEngine.UI.Selectable
    {
        public class OptionData { public string text { get; set; } }
        public class DropdownEvent : UnityEngine.Events.UnityEvent<int> { }
        public int value { get; set; }
        public TMP_Text captionText { get; set; }
        public System.Collections.Generic.List<OptionData> options { get; set; } = new();
        public DropdownEvent onValueChanged { get; set; } = new();
        public void SetValueWithoutNotify(int value) { }
        public void RefreshShownValue() { }
        public void Show() { }
        public void Hide() { }
        public void ClearOptions() { }
        public void AddOptions(System.Collections.Generic.List<OptionData> options) { }
    }
}
