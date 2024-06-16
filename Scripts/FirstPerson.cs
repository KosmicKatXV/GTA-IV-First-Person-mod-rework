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

        public void reset()
        {
            vel = new float[] { 0, 0 };
            pos = new float[] { 0, 0 };
            lim = new float[] { 2, 1 };
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

        public void setLim(float[] newLim)
        {
            lim = newLim;
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
        Boolean aiming,driving,ragdoll, camPressed;
        Vector3 offsetVector;
        String state;
        public FirstPerson()
        {
            sight = Resources.GetTexture("hud_crosshair.png");
            //personSight = Resources.GetTexture("hud_target");

            fpCam = initCamera();
            camActivated = false;
            mHandler = new MouseHandler();
            state = null;

            this.Interval = 0;
            this.Tick += new EventHandler(camEvent);
            this.KeyDown += new GTA.KeyEventHandler(keyHandler);
            this.PerFrameDrawing += new GTA.GraphicsEventHandler(sightDrawing);
            
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
                    mHandler.reset();
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
            Game.DisplayText(ragdoll.ToString());
            if (camActivated)
            {
                mHandler.update();
                sinDir = (float)-Math.Sin((Math.PI / 180) * Player.Character.Heading);
                cosDir = (float)Math.Cos((Math.PI / 180) * Player.Character.Heading);
                aiming = Game.isGameKeyPressed(GameKey.Aim);
                driving = Player.Character.isInVehicle();
                ragdoll = GTA.Native.Function.Call<Boolean>("IS_PED_RAGDOLL",Player.Character);
                if (driving)
                {
                    if (aiming) camAimCar(sinDir, cosDir);
                    else camCar(sinDir, cosDir);
                }
                else
                {
                    if (aiming) camAimFoot(sinDir, cosDir);
                    if (ragdoll) camRagdoll(sinDir, cosDir);
                    else camFoot(sinDir, cosDir);
                }
            }
        }

        private void camRagdoll(float sin, float cos)
        {
            if (state != "ragdoll")
            {
                mHandler.reset();
                state = "ragdoll";
            }
            float Zangle = currentArmAngle(offsetVector);
            fpCam.Direction = Player.Character.Direction + new Vector3(0, 0, Zangle);
            fpCam.Position = Player.Character.GetBonePosition(Bone.Head);
        }

        private void camFoot(float sin, float cos)
        {
            if(state != "foot")
            {
                mHandler.reset();
                state = "foot";
            }
            float offset = -0.05f;
            Player.Character.Heading -= mHandler.velX();
            fpCam.Heading = Player.Character.Heading;
            fpCam.Direction = Player.Character.Direction + new Vector3(0, 0, -mHandler.posY());
            fpCam.Position = Player.Character.GetBonePosition(Bone.Head) + new Vector3(sin * offset, cos * offset,0.1f);
        }

        private void camAimFoot(float sin, float cos)
        {
            if (state != "aimFoot")
            {
                mHandler.reset();
                state = "aimFoot";
            }
            float offsetX = -0.05f;
            float offsetZ = 0.1f;
            offsetVector = new Vector3(sin * offsetX, cos * offsetX, offsetZ);
            float Zangle = currentArmAngle(offsetVector);
            //fpCam.Direction = Player.Character.Direction - new Vector3(0, 0, Player.Character.Direction.Z);
            fpCam.Direction = Player.Character.Direction + new Vector3(0, 0, Zangle);
            //fpCam.Heading = Player.Character.Heading - 6;
            //Game.DisplayText((Player.Character.Heading - fpCam.Heading).ToString());
            fpCam.Position = Player.Character.GetBonePosition(Bone.Head) + offsetVector;
        }

        private void camCar(float sin, float cos)
        {
            if (state != "car")
            {
                mHandler.setLim(new float[] {20,1});
                state = "car";
            }
            float offset = 1.0f;
            //GTA.Native.Function.Call("SET_CAR_FORWARD_SPEED", Player.Character.CurrentVehicle, 0);
            offsetVector = Player.Character.GetBonePosition(Bone.Head) - Player.Character.Position;
            fpCam.Position = Player.Character.Position + offsetVector;
            fpCam.Direction = Player.Character.Direction + new Vector3(0, 0, -mHandler.posY()*offset);
            fpCam.Heading += mHandler.posX() * 4;
            Game.DisplayText(offsetVector.ToString());
            //fpCam.Rotation = Player.Character.CurrentVehicle.Rotation;
        }

        private void camAimCar(float sin, float cos)
        {
            if (state != "aimCar")
            {
                state = "aimCar";
            }
        }

        private float currentArmAngle(Vector3 offset)
        {
            Vector3 head2hand = Player.Character.GetBonePosition(Bone.HDFaceTogueJointA) - (Player.Character.GetBonePosition(Bone.Head) + offset);
            float output = ((((float)((head2hand.Z + offset.Z) / 0.14))) + 0.14f) * 2;
            return output;
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
            if (aiming)
            {
                float size = Game.Resolution.Width * 0.013f;
                // calculate the center of the radar

                float radarCenterX = Game.Resolution.Width / 2;
                float radarCenterY = Game.Resolution.Height / 2;


                e.Graphics.Scaling = FontScaling.Pixel;
                e.Graphics.DrawSprite(sight, radarCenterX - size / 2, radarCenterY - size / 2, size, size, 0); //upLeft
                e.Graphics.DrawSprite(sight, radarCenterX - size / 2, radarCenterY + size / 2, size, size, -(float)Math.PI / 2); //upRight
                e.Graphics.DrawSprite(sight, radarCenterX + size / 2, radarCenterY - size / 2, size, size, (float)Math.PI / 2); //botLeft
                e.Graphics.DrawSprite(sight, radarCenterX + size / 2, radarCenterY + size / 2, size, size, (float)Math.PI); //botRight
            }
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