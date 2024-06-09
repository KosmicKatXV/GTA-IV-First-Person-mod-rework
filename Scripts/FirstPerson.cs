using GTA;
using GTA.@base;
using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.Remoting;
using System.Security.AccessControl;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FirstPerson.net
{
    public class MouseHandler : Script
    {
        float[] vel,pos,lim;

        GTA.Native.Pointer  ptr_x,
                            ptr_y;

        private float xDiff = 20,
                        yDiff = 1000;
            
        public MouseHandler()
        {
            vel = new float[] {0,0};
            pos = new float[] {0,0};
            lim = new float[] {2,1};

            ptr_x = new GTA.Native.Pointer(typeof(int));
            ptr_y = new GTA.Native.Pointer(typeof(int));
        }

        public void update()
        {
            GTA.Native.Function.Call("GET_MOUSE_INPUT", ptr_x, ptr_y);
            vel = new float[]{ptr_x/xDiff, ptr_y/yDiff};
            pos = new float[] { pos[0] - vel[0], pos[1] + vel[1] };
            for (int i = 0; i < lim.Length; i++)
            {
                if (pos[i] > lim[i]) pos[i] = lim[i];
                if (pos[i] < -lim[i]) pos[i] = -lim[i];
            }
        }

        public void resetPos()
        {
            pos = new float[] { 0, 0 };
        }

        public float posX()
        {
            return pos[0];
        }
        public float posY()
        {
            return pos[1];
        }

        public float velX()
        {
            return vel[0];
        }

        public float velY()
        {
            return vel[1];
        }
    }

    public class FirstPerson : Script
    {
        private Texture sight;
        //private Texture personSight;

        GTA.Camera fpCam;
        MouseHandler mHandler;
        bool camActivated;
        float sinDir, cosDir;
        Boolean aiming, camPressed;

        public FirstPerson()
        {
            sight = Resources.GetTexture("hud_crosshair.png");
            //personSight = Resources.GetTexture("hud_target");
            fpCam = initCamera();
            camActivated = false;
            mHandler = new MouseHandler();
            //BindKey(Keys.B, new KeyPressDelegate(changeCamera));
            //BindKey(Keys.N, new KeyPressDelegate(destroyCamera));
            this.Interval = 0;
            this.Tick += new EventHandler(camEvent);
            this.KeyDown += new GTA.KeyEventHandler(keyHandler);
        }

        private void keyHandler(object sender, GTA.KeyEventArgs e)
        {
            if (Keys.B == e.Key)
            {
                camActivated = !camActivated;
                if (camActivated)
                {
                    fpCam = initCamera();
                    fpCam.isActive = true;
                    mHandler.resetPos();
                    GTA.Native.Function.Call("SET_DRAW_PLAYER_COMPONENT", 0, 0);
                    GTA.Native.Function.Call("SET_DRAW_PLAYER_COMPONENT", 7, 0);
                    GTA.Native.Function.Call("SET_DRAW_PLAYER_COMPONENT", 9, 0);
                }
                else
                {
                    fpCam.isActive = false;
                    GTA.Native.Function.Call("SET_DRAW_PLAYER_COMPONENT", 0, 1);
                    GTA.Native.Function.Call("SET_DRAW_PLAYER_COMPONENT", 7, 1);
                    GTA.Native.Function.Call("SET_DRAW_PLAYER_COMPONENT", 9, 1);
                }
            }
        }

        private void camEvent(object sender, EventArgs e)
        {
            Game.DisplayText(camActivated.ToString());
            if (camActivated)
            {
                mHandler.update();
                sinDir = (float)-Math.Sin((Math.PI / 180) * Player.Character.Heading);
                cosDir = (float)Math.Cos((Math.PI / 180) * Player.Character.Heading);
                aiming = Game.isGameKeyPressed(GameKey.Aim);
                //camPressed = Game.isGameKeyPressed(GameKey.);
                if (Player.Character.isInVehicle())
                {
                    if (aiming) camAimCar(sinDir, cosDir);
                    else camCar(sinDir, cosDir);
                }
                else
                {
                    if (aiming) camAimFoot(sinDir, cosDir);
                    else camFoot(sinDir, cosDir);
                }
            }
        }

        private void camFoot(float sin, float cos)
        {
            float offset = -0.05f;
            Player.Character.Heading -= mHandler.velX();
            fpCam.Heading = Player.Character.Heading;
            fpCam.Direction = Player.Character.Direction + new Vector3(0, 0, -mHandler.posY());
            fpCam.Position = Player.Character.GetBonePosition(Bone.Head) + new Vector3(sin * offset, cos * offset,0.1f);
        }

        private void camAimFoot(float sin, float cos)
        {
            float offset = -0.05f;
            //GTA.Object holding = World.CreateObject(new Model(1862763509), new Vector3(0,0,0));
            //GTA.Native.Function.Call("GET_OBJECT_PED_IS_HOLDING", Player.Character, holding);
            //Game.DisplayText(holding.Model.ToString());
            fpCam.Heading = Player.Character.Heading;
            //fpCam.LookAt(Player.Character.h);
            //GTA.Native.Function.Call("ATTACH_CAM_TO_PED", fpCam, Player.Character);
            //fpCam.Direction = Player.Character.Direction + new Vector3(0, 0, -mHandler.posY());
            fpCam.Position = Player.Character.GetBonePosition(Bone.Head) + new Vector3(sin * offset, cos * offset, 0.1f);
        }

        private void camCar(float sin, float cos)
        {
            float offset = 0.0f;
            fpCam.Position = Player.Character.GetBonePosition(Bone.Head) + Player.Character.CurrentVehicle.Velocity / 100;
            fpCam.Direction = Player.Character.Direction + new Vector3(mHandler.posX()*offset, 0, mHandler.posY()*offset);
            fpCam.Rotation = Player.Character.CurrentVehicle.Rotation;
        }

        private void camAimCar(float sin, float cos)
        {

        }

        private GTA.Camera initCamera()
        {
            GTA.Camera output = new GTA.Camera();
            output.DrunkEffectIntensity = 0f;
            output.FOV = Game.CurrentCamera.FOV;
            output.Heading = Player.Character.Heading;
            output.Position = Player.Character.Position;
            output.Direction = Player.Character.Direction;
            return output;
        }

        private void sightDrawing(System.Object sender, GTA.GraphicsEventArgs e)
        {
            float size = Game.Resolution.Width*0.013f;
            // calculate the center of the radar
            
            float radarCenterX = Game.Resolution.Width / 2;
            float radarCenterY = Game.Resolution.Height / 2;
            

            e.Graphics.Scaling = FontScaling.Pixel;
            e.Graphics.DrawSprite(sight, radarCenterX - size / 2, radarCenterY - size / 2, size, size, 0); //upLeft
            e.Graphics.DrawSprite(sight, radarCenterX - size / 2, radarCenterY + size / 2, size, size, -(float)Math.PI/2); //upRight
            e.Graphics.DrawSprite(sight, radarCenterX + size / 2, radarCenterY - size / 2, size, size, (float)Math.PI/2); //botLeft
            e.Graphics.DrawSprite(sight, radarCenterX + size / 2, radarCenterY + size / 2, size, size, (float)Math.PI); //botRight
        }

        private void destroyCamera()
        {
            GTA.Native.Function.Call("SET_DRAW_PLAYER_COMPONENT", 0, 1);
            GTA.Native.Function.Call("SET_DRAW_PLAYER_COMPONENT", 7, 1);
            GTA.Native.Function.Call("SET_DRAW_PLAYER_COMPONENT", 9, 1);
            //GTA.Native.Function.Call("SET_DRAW_PLAYER_COMPONENT", 10, 1);
            GTA.Native.Function.Call("SET_CAM_ACTIVE", fpCam, 0);
            GTA.Native.Function.Call("SET_CAM_PROPAGATE", Game.DefaultCamera, 1);
            fpCam.Delete();
            GTA.Native.Function.Call("SET_GAME_CAMERA_CONTROLS_ACTIVE", 1);
        }
    }
}