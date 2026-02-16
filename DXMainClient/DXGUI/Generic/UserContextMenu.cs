using ClientCore.Extensions;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAClient.DXGUI.Generic
{
    public class UserContextMenu : XNAPanel
    {
        private readonly Action _onEditProfile;

        private XNAClientButton btnEditProfile;

        public UserContextMenu(WindowManager windowManager, Action onEditProfile) : base(windowManager)
        {
            _onEditProfile = onEditProfile;
        }

        public override void Initialize()
        {
            Name = nameof(UserContextMenu);
            ClientRectangle = new Rectangle(0, 0, 150, 30);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(40, 40, 40, 240), 1, 1);
            DrawBorders = true;
            BorderColor = Color.Gray;

            btnEditProfile = new XNAClientButton(WindowManager);
            btnEditProfile.Name = nameof(btnEditProfile);
            btnEditProfile.Text = "Edit Profile".L10N("Client:Main:EditProfile");
            btnEditProfile.ClientRectangle = new Rectangle(5, 3, 140, 24);
            btnEditProfile.LeftClick += BtnEditProfile_LeftClick;
            AddChild(btnEditProfile);

            base.Initialize();

            Disable();
        }

        private void BtnEditProfile_LeftClick(object sender, EventArgs e)
        {
            Disable();
            _onEditProfile?.Invoke();
        }

        public void Open(Point location)
        {
            ClientRectangle = new Rectangle(location.X, location.Y, 150, 30);
            Enable();
        }
    }
}
