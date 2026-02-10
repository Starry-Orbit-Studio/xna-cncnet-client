using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Rampastring.Tools;
using System;
using ClientCore;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using System.Linq;

namespace ClientGUI
{
    public enum ActiveMode
    {
        Normal,
        NotTransparencyPixel,
        Polygon,
    }

    public class XNAClientButton : XNAButton, IToolTipContainer
    {
        public ToolTip ToolTip { get; private set; }

        private bool _lastEnterStatus = false;
        private readonly static WeakReference<Color[]> _colorsCache = new([]);
        private string _initialToolTipText;
        public string ToolTipText
        {
            get => Initialized ? ToolTip?.Text : _initialToolTipText;
            set
            {
                if (Initialized)
                    ToolTip.Text = value;
                else
                    _initialToolTipText = value;
            }
        }
        public ActiveMode ActiveMode { get; set; } = ActiveMode.Normal;

        private Point[] _polygonPoints = [];

        public XNAClientButton(WindowManager windowManager) : base(windowManager)
        {
            FontIndex = 1;
            Height = UIDesignConstants.BUTTON_HEIGHT;
        }

        public override void Initialize()
        {
            int width = Width;

            if (IdleTexture == null)
                IdleTexture = AssetLoader.LoadTexture(width + "pxbtn.png");

            if (HoverTexture == null)
                HoverTexture = AssetLoader.LoadTexture(width + "pxbtn_c.png");

            if (HoverSoundEffect == null)
                HoverSoundEffect = new EnhancedSoundEffect("button.wav");

            base.Initialize();

            if (Width == 0)
                Width = IdleTexture.Width;

            ToolTip = new ToolTip(WindowManager, this) { Text = _initialToolTipText };
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "MatchTextureSize" when Conversions.BooleanFromString(value, false):
                    Width = IdleTexture.Width;
                    Height = IdleTexture.Height;
                    return;
                case "ToolTip":
                    ToolTipText = value.FromIniString();
                    return;
                case nameof(ActiveMode) when Enum.TryParse<ActiveMode>(value, out var activeMode):
                    ActiveMode = activeMode;
                    return;
                case "PolygonPoints":
                    _polygonPoints = value
                        .Split(':')
                        .Select(s => s.Split(','))
                        .Select(a => new Point(
                            int.Parse(a[0].Trim()),
                            int.Parse(a[1].Trim())))
                        .ToArray();
                    return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void OnMouseEnter()
        {
            switch (ActiveMode)
            {
                case ActiveMode.NotTransparencyPixel:
                case ActiveMode.Polygon:
                    var current = CheckActive();
                    if ((current, _lastEnterStatus) is (true, false))
                        base.OnMouseEnter();
                    _lastEnterStatus = current;
                    break;
                case ActiveMode.Normal:
                default:
                    base.OnMouseEnter();
                    break;
            }
        }

        public override void OnMouseLeave()
        {
            switch (ActiveMode)
            {
                case ActiveMode.NotTransparencyPixel:
                case ActiveMode.Polygon:
                    var current = CheckActive();
                    if ((current, _lastEnterStatus) is (false, true))
                        base.OnMouseLeave();
                    _lastEnterStatus = current;
                    break;
                case ActiveMode.Normal:
                default:
                    base.OnMouseLeave();
                    break;
            }
        }

        public override void OnMouseMove()
        {
            switch (ActiveMode)
            {
                case ActiveMode.NotTransparencyPixel:
                case ActiveMode.Polygon:
                    var current = CheckActive();
                    switch ((current, _lastEnterStatus))
                    {
                        case (true, true):
                            base.OnMouseMove();
                            _lastEnterStatus = current;
                            break;
                        case (false, true):
                            OnMouseLeave();
                            break;
                        case (true, false):
                            OnMouseEnter();
                            break;
                        case (false, false):
                        default:
                            _lastEnterStatus = current;
                            break;
                    }

                    break;
                case ActiveMode.Normal:
                default:
                    base.OnMouseMove();
                    break;
            }
        }

        public override void OnLeftClick(InputEventArgs inputEventArgs)
        {
            switch (ActiveMode)
            {
                case ActiveMode.NotTransparencyPixel:
                case ActiveMode.Polygon:
                    var current = CheckActive();
                    if (current)
                        base.OnLeftClick(inputEventArgs);
                    break;
                case ActiveMode.Normal:
                default:
                    base.OnLeftClick(inputEventArgs);
                    break;
            }
        }

        private bool CheckActive()
        {
            switch (ActiveMode)
            {
                case ActiveMode.NotTransparencyPixel:
                    {
                        Point checkPoint = GetCursorPoint();
                        if (!IdleTexture.Bounds.Contains(checkPoint))
                            return false;

                        var size = IdleTexture.Width * IdleTexture.Height;
                        _colorsCache.TryGetTarget(out var color);
                        if (color is null || color.Length < size)
                            color = new Color[size];
                        _colorsCache.SetTarget(color);

                        var index = IdleTexture.Width * checkPoint.Y + checkPoint.X;
                        IdleTexture.GetData(color);

                        return color[index].A != 0;
                    }
                case ActiveMode.Polygon:
                    {
                        Point checkPoint = GetCursorPoint();
                        bool inside = false;

                        int pointCount = _polygonPoints.Length;
                        for (int i = 0, j = pointCount - 1; i < pointCount; j = i, i++)
                        {
                            ref Point p1 = ref _polygonPoints[i];
                            ref Point p2 = ref _polygonPoints[j];
                            if (checkPoint.Y < p2.Y)
                            {
                                if (p1.Y <= checkPoint.Y)
                                {
                                    if ((checkPoint.Y - p1.Y) * (p2.X - p1.X) > (checkPoint.X - p1.X) * (p2.Y - p1.Y))
                                    {
                                        inside = !inside;
                                    }
                                }
                            }
                            else if (checkPoint.Y < p1.Y)
                            {
                                if ((checkPoint.Y - p1.Y) * (p2.X - p1.X) < (checkPoint.X - p1.X) * (p2.Y - p1.Y))
                                {
                                    inside = !inside;
                                }
                            }
                        }

                        return inside;
                    }
                default:
                    return true;
            }

        }
    }
}
