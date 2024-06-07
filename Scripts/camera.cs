/**********************************************/
/*      file:           main.cs
 *      part of:        Camera-Control Mod
 *      author:         thaCURSEDpie
 *      creation date:  2010-05-15
 *      
 *      description:
 *          Control the in-game camera.
 *          (no-clipping for example)
 *          
 *          
 *          
/*
/**********************************************/

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.Media;
using GTA;

namespace cameras
{
    class cameraScript : Script
    {
        #region variable declaration
        struct keyStruct
        {
            public Keys key;
            public bool pressed;
            public string name;

            public keyStruct(Keys aKey, string keyName)
            {
                key = aKey;
                name = keyName;
                pressed = false;
            }
            public keyStruct(Keys theKey, string keyName, bool isPressed)
            {
                name = keyName;
                key = theKey;
                pressed = false;
            }

        }

        enum numToKey
        {
            forwardKey = 0,
            backKey = 1,
            rightKey = 2,
            leftKey = 3,
            strafeUpKey = 4,
            strafeDownKey = 5,
            strafeLeftKey = 6,
            strafeRightKey = 7,
            upwardKey = 8,
            downwardKey = 9,
            sprintKey = 10,
            incFov = 11,
            decFov = 12,
            incRoll = 13,
            decRoll = 14,
            yForwardKey = 15,
            yBackwardKey = 16,
            carCam_positionKey = 17
        }

        Keys camKey = Keys.F4;

        keyStruct[] keyArray = new keyStruct[100];

        int numOfKeys,
            loadTest;

        bool customCamEnabled = false,
             customCarCamEnabled = false,
             useCameraObject = false,
             idlePosBool = false,
             inheritVehicleRoll = false,
             headingAdjustBool = false,
             isAdjustingCarCamPosition = false,
             inheritCarRoll = true;

        float maxHeadingDiff = (float)(Math.PI/2f),
              headingReturnMult = 1.0f,
              forwardMult = 0.5f,
              backMult = 0.5f,
              leftMult = 0.5f,
              rightMult = 0.5f,
              upMult = 1.0f,
              downMult = 1.0f,
              strafeLmult = 1.0f,
              strafeRmult = 1.0f,
              strafeUmult = 0.05f,
              strafeDmult = 0.05f,
              sprintMult = 4.0f,
              mouseXmult = 140f,
              mouseYmult = 2.0f,
              fovMult = 1.0f,
              rollMult = 1.0f; // 1 : 80 = mouseYmult : mouseXmult

        float camX,
              camY,
              camZ,
              camXvel,
              camYvel,
              camZvel,
              camHeX,
              camHeY,
              camHeZ,
              camForwardVel,
              camHeading,
              camFov,
              camAngle,
              camMaxYdeviation,
              camMaxXdeviation,
              customCarCam_headingDiff;
     
        GTA.Camera customCam,
                   customCarCam,
                   customCamIdle;

        Vector3 startPoint,
                oldVehPos,
                camOldPos,
                customCarCam_offsPos,
                customCarCam_aimCoord,
                customCarCam_validOffsPos,
                customCarCam_directionDiff,
                defaultCarCamPos;
        #endregion

        #region constructor
        public cameraScript()
        {
            keyArray[0] = new keyStruct(Keys.W, "forwardKey");
            keyArray[1] = new keyStruct(Keys.S, "backKey");
            keyArray[2] = new keyStruct(Keys.D, "rightKey");
            keyArray[3] = new keyStruct(Keys.A, "leftKey");
            keyArray[4] = new keyStruct(Keys.Up, "strafeUpKey");
            keyArray[5] = new keyStruct(Keys.Down, "strafeDownKey");
            keyArray[6] = new keyStruct(Keys.Left, "strafeLeftKey");
            keyArray[7] = new keyStruct(Keys.Right, "strafeRightKey");
            keyArray[8] = new keyStruct(Keys.E, "upwardKey");
            keyArray[9] = new keyStruct(Keys.Q, "downwardKey");
            keyArray[10] = new keyStruct(Keys.ShiftKey, "sprintKey");
            keyArray[11] = new keyStruct(Keys.F5, "incFov");
            keyArray[12] = new keyStruct(Keys.F6, "decFov");
            keyArray[13] = new keyStruct(Keys.F7, "incRoll");
            keyArray[14] = new keyStruct(Keys.F8, "decRoll");
            keyArray[15] = new keyStruct(Keys.X, "yForwardKey");
            keyArray[16] = new keyStruct(Keys.Z, "yBackwardKey");
            keyArray[17] = new keyStruct(Keys.F3, "carCam_positionKey");

            numOfKeys = 18;

            oldVehPos = new Vector3();
            startPoint = new Vector3();
            customCam = new GTA.Camera();
            customCarCam = new GTA.Camera();
            customCamIdle = new GTA.Camera();            
            customCarCam_aimCoord = new Vector3(0, 3f, 0f);
            customCarCam_directionDiff = new Vector3();
            defaultCarCamPos = new Vector3(-1.3f, 0f, 0f);
            customCarCam_offsPos = defaultCarCamPos;
            Interval = 0;

            this.KeyDown += new GTA.KeyEventHandler(bombScript_KeyDown);
            this.KeyUp += new GTA.KeyEventHandler(cameraScript_KeyUp);
            this.Tick += new EventHandler(bombScript_Tick);

            BindConsoleCommand("camcontrol_reloadsettings", new ConsoleCommandDelegate(console_reloadSettings), "- Reload the Cam-Control settings.");
            loadSettings();
        }
        #endregion

        #region settings loading
        private void console_reloadSettings(ParameterCollection Parameter) // test SET_DRUNK_CAM
        {
            loadSettings();
        }

        private void loadSettings()
        {
            Settings.Load();
            for (int i = 0; i < numOfKeys; i++)
            {
                keyArray[i].key = Settings.GetValueKey(keyArray[i].name, "keys", keyArray[i].key);
            }
            camKey = Settings.GetValueKey("activationKey", "keys", Keys.F4);
            forwardMult = Settings.GetValueFloat("forwardMult", "modifiers", 0.5f);
            backMult = Settings.GetValueFloat("backMult", "modifiers", 0.5f);
            leftMult = Settings.GetValueFloat("leftMult", "modifiers", 0.5f);
            rightMult = Settings.GetValueFloat("rightMult", "modifiers", 0.5f);
            upMult = Settings.GetValueFloat("upMult", "modifiers", 1.0f);
            downMult = Settings.GetValueFloat("downMult", "modifiers", 1.0f);
            strafeLmult = Settings.GetValueFloat("strafeLmult", "modifiers", 3.0f);
            strafeRmult = Settings.GetValueFloat("strafeRmult", "modifiers", 3.0f);
            strafeUmult = Settings.GetValueFloat("strafeUmult", "modifiers", 0.05f);
            strafeDmult = Settings.GetValueFloat("strafeDmult", "modifiers", 0.05f);
            sprintMult = Settings.GetValueFloat("sprintMult", "modifiers", 0.05f);
            mouseXmult = Settings.GetValueFloat("mouseXmult", "modifiers", 140f);
            mouseYmult = Settings.GetValueFloat("mouseYmult", "modifiers", 2.0f);
            fovMult = Settings.GetValueFloat("fovMult", "modifiers", 1.0f);
            rollMult = Settings.GetValueFloat("rollMult", "modifiers", 1.0f); ; // 1 : 80 = mouseYmult : mouseXmult
            loadTest = Settings.GetValueInteger("loadTest", "donttouch", -1);
            if (loadTest == -1)
            {
                Game.Console.Print("[Cam-Control]: failed to load settings");
            }
        }

        #endregion

        #region main
        private void handleInputCamObj()
        {
            float fpsMult = 20 / Game.FPS;

            if (keyArray[(int)numToKey.incFov].pressed)
            {
                if (customCam.FOV < 170)
                {
                    customCam.FOV += fovMult;
                }
            }
            if (keyArray[(int)numToKey.decFov].pressed)
            {
                if (customCam.FOV > 3)
                {
                    customCam.FOV -= fovMult;
                }
            }
            if (keyArray[(int)numToKey.incRoll].pressed)
            {
                if (customCam.Roll < 89)
                {
                    customCam.Roll += rollMult;
                }
                else
                {
                    customCam.Roll = -89;
                }
            }
            if (keyArray[(int)numToKey.decRoll].pressed)
            {
                if (customCam.Roll > -89)
                {
                    customCam.Roll -= rollMult;
                }
                else
                {
                    customCam.Roll = 89;
                }
            }

            if (keyArray[(int)numToKey.forwardKey].pressed)
            {
                if (keyArray[(int)numToKey.sprintKey].pressed)
                {
                    camForwardVel = 24f;
                }
                else
                {
                    camForwardVel = 6.0f;
                }
            }
            else if (keyArray[(int)numToKey.backKey].pressed)
            {
                if (keyArray[(int)numToKey.sprintKey].pressed)
                {
                    camForwardVel = -24f;
                }
                else
                {
                    camForwardVel = -6.0f;
                }
            }
            else
            {
                camForwardVel = 0;
            }

            if (keyArray[(int)numToKey.leftKey].pressed)
            {
                float tempFloat = camHeading - 180;

                camXvel = -6.0f;
            }
            else if (keyArray[(int)numToKey.rightKey].pressed)
            {
                float tempFloat = camHeading;

                camXvel = 6.0f;
            }
            else
            {
                camXvel = 0;
            }

            if (keyArray[(int)numToKey.upwardKey].pressed)
            {
                camZvel = 12.0f;

            }
            else if (keyArray[(int)numToKey.downwardKey].pressed)
            {
                camZvel = -12.0f;
            }
            else
            {
                camZvel = 0.0f;
            }

            if (keyArray[(int)numToKey.strafeDownKey].pressed)
            {
                camHeZ -= strafeDmult * fpsMult;
                if (camHeZ < -1.0f)
                {
                    camHeZ = -1.0f;
                }
            }

            if (keyArray[(int)numToKey.strafeUpKey].pressed)
            {
                camHeZ += strafeUmult * fpsMult;
                if (camHeZ > 1.0f)
                {
                    camHeZ = 1.0f;
                }
            }

            if (keyArray[(int)numToKey.strafeLeftKey].pressed)
            {
                camHeading += strafeLmult * fpsMult;
            }
            if (keyArray[(int)numToKey.strafeRightKey].pressed)
            {
                camHeading -= strafeRmult * fpsMult;
            }

            if (!keyArray[(int)numToKey.strafeLeftKey].pressed
                && !keyArray[(int)numToKey.strafeRightKey].pressed
                && !keyArray[(int)numToKey.strafeUpKey].pressed
                && !keyArray[(int)numToKey.strafeDownKey].pressed)
            {
                PointF mouseMov = Game.Mouse.Movement;
                if (mouseMov.X != 0)
                {
                    camHeading -= mouseMov.X * mouseXmult * fpsMult;
                }
                if (mouseMov.Y != 0)
                {
                    camHeZ -= mouseMov.Y * mouseYmult * fpsMult;

                    if (camHeZ > 1.0f)
                    {
                        camHeZ = 1.0f;
                    }
                    else if (camHeZ < -1.0f)
                    {
                        camHeZ = -1.0f;
                    }
                }
            }

        }
        private void handleInput()
        {
            Game.Console.Print("test2");
            if (keyArray[(int)numToKey.incFov].pressed)
            {
                if (customCam.FOV < 170)
                {
                    customCam.FOV += fovMult;
                }
            }
            if (keyArray[(int)numToKey.decFov].pressed)
            {
                if (customCam.FOV > 3)
                {
                    customCam.FOV -= fovMult;
                }
            }
            if (keyArray[(int)numToKey.incRoll].pressed)
            {
                if (customCam.Roll < 89)
                {
                    customCam.Roll += rollMult;
                }
                else
                {
                    customCam.Roll = -89;
                }
            }
            if (keyArray[(int)numToKey.decRoll].pressed)
            {
                if (customCam.Roll > -89)
                {
                    customCam.Roll -= rollMult;
                }
                else
                {
                    customCam.Roll = 89;
                }
            }
            if (keyArray[(int)numToKey.forwardKey].pressed)
            {
                if (keyArray[(int)numToKey.sprintKey].pressed)
                {
                    camX += camHeX * forwardMult * sprintMult;
                    camY += camHeY * forwardMult * sprintMult;
                    camZ += camHeZ * forwardMult * sprintMult;
                }
                else
                {
                    Game.Console.Print("test3");
                    camX += camHeX * forwardMult;
                    camY += camHeY * forwardMult;
                    camZ += camHeZ * forwardMult;
                }
            }
            if (keyArray[(int)numToKey.backKey].pressed)
            {
                if (keyArray[(int)numToKey.sprintKey].pressed)
                {
                    camX -= camHeX * backMult * sprintMult;
                    camY -= camHeY * backMult * sprintMult;
                    camZ -= camHeZ * backMult * sprintMult;
                }
                else
                {
                    camX -= camHeX * backMult;
                    camY -= camHeY * backMult;
                    camZ -= camHeZ * backMult;
                }
            }
            if (keyArray[(int)numToKey.leftKey].pressed)
            {
                float tempFloat = camHeading - 180;

                camX += (float)Math.Cos((Math.PI / 180) * tempFloat) * leftMult;
                camY += (float)Math.Sin((Math.PI / 180) * tempFloat) * leftMult;
            }
            if (keyArray[(int)numToKey.rightKey].pressed)
            {
                float tempFloat = camHeading;

                camX += (float)Math.Cos((Math.PI / 180) * tempFloat) * rightMult;
                camY += (float)Math.Sin((Math.PI / 180) * tempFloat) * rightMult;
            }
            if (keyArray[(int)numToKey.upwardKey].pressed)
            {
                camZ += upMult;
            }
            if (keyArray[(int)numToKey.downwardKey].pressed)
            {
                camZ -= downMult;
            }

            if (keyArray[(int)numToKey.strafeDownKey].pressed)
            {
                camHeZ -= strafeDmult;
                if (camHeZ < -1.0f)
                {
                    camHeZ = -1.0f;
                }
            }

            if (keyArray[(int)numToKey.strafeUpKey].pressed)
            {
                camHeZ += strafeUmult;
                if (camHeZ > 1.0f)
                {
                    camHeZ = 1.0f;
                }
            }

            if (keyArray[(int)numToKey.strafeLeftKey].pressed)
            {
                camHeading += strafeLmult;
            }
            if (keyArray[(int)numToKey.strafeRightKey].pressed)
            {
                camHeading -= strafeRmult;
            }

            if (!keyArray[(int)numToKey.strafeLeftKey].pressed
                && !keyArray[(int)numToKey.strafeRightKey].pressed
                && !keyArray[(int)numToKey.strafeUpKey].pressed
                && !keyArray[(int)numToKey.strafeDownKey].pressed)
            {
                
                PointF mouseMov = Game.Mouse.Movement;
                if (mouseMov.X != 0)
                {
                    camHeading -= mouseMov.X * mouseXmult;
                }
                if (mouseMov.Y != 0)
                {
                    camHeZ -= mouseMov.Y * mouseYmult;

                    if (camHeZ > 1.0f)
                    {
                        camHeZ = 1.0f;
                    }
                    else if (camHeZ < -1.0f)
                    {
                        camHeZ = -1.0f;
                    }
                }
            }

        }
        private void handleInput_car()
        {
            if (keyArray[(int)numToKey.incFov].pressed)
            {
                if (customCarCam.FOV < 170)
                {
                    customCarCam.FOV += fovMult;
                }
            }
            if (keyArray[(int)numToKey.decFov].pressed)
            {
                if (customCarCam.FOV > 3)
                {
                    customCarCam.FOV -= fovMult;
                }
            }
            if (keyArray[(int)numToKey.incRoll].pressed)
            {
                if (customCarCam.Roll < 89)
                {
                    customCarCam.Roll += rollMult;
                }
                else
                {
                    customCarCam.Roll = -89;
                }
            }
            if (keyArray[(int)numToKey.decRoll].pressed)
            {
                if (customCarCam.Roll > -89)
                {
                    customCarCam.Roll -= rollMult;
                }
                else
                {
                    customCarCam.Roll = 89;
                }
            }
            if (keyArray[(int)numToKey.forwardKey].pressed)
            {
                if (keyArray[(int)numToKey.sprintKey].pressed)
                {
                    camX += camHeX * forwardMult * sprintMult;
                    camY += camHeY * forwardMult * sprintMult;
                    camZ += camHeZ * forwardMult * sprintMult;
                }
                else
                {
                    camX += camHeX * forwardMult;
                    camY += camHeY * forwardMult;
                    camZ += camHeZ * forwardMult;
                }
            }
            if (keyArray[(int)numToKey.backKey].pressed)
            {
                if (keyArray[(int)numToKey.sprintKey].pressed)
                {
                    camX -= camHeX * backMult * sprintMult;
                    camY -= camHeY * backMult * sprintMult;
                    camZ -= camHeZ * backMult * sprintMult;
                }
                else
                {
                    camX -= camHeX * backMult;
                    camY -= camHeY * backMult;
                    camZ -= camHeZ * backMult;
                }
            }
            if (keyArray[(int)numToKey.leftKey].pressed)
            {
                float tempFloat = camHeading - 180;

                camX += (float)Math.Cos((Math.PI / 180) * tempFloat) * leftMult;
                camY += (float)Math.Sin((Math.PI / 180) * tempFloat) * leftMult;
            }
            if (keyArray[(int)numToKey.rightKey].pressed)
            {
                float tempFloat = camHeading;

                camX += (float)Math.Cos((Math.PI / 180) * tempFloat) * rightMult;
                camY += (float)Math.Sin((Math.PI / 180) * tempFloat) * rightMult;
            }
            if (keyArray[(int)numToKey.upwardKey].pressed)
            {
                camZ += upMult;
            }
            if (keyArray[(int)numToKey.downwardKey].pressed)
            {
                camZ -= downMult;
            }

            if (keyArray[(int)numToKey.strafeDownKey].pressed)
            {
                camHeZ -= strafeDmult;
                if (camHeZ < -1.0f)
                {
                    camHeZ = -1.0f;
                }
            }

            if (keyArray[(int)numToKey.strafeUpKey].pressed)
            {
                camHeZ += strafeUmult;
                if (camHeZ > 1.0f)
                {
                    camHeZ = 1.0f;
                }
            }

            if (keyArray[(int)numToKey.strafeLeftKey].pressed)
            {
                camHeading += strafeLmult;
            }
            if (keyArray[(int)numToKey.strafeRightKey].pressed)
            {
                camHeading -= strafeRmult;
            }

            if (!keyArray[(int)numToKey.strafeLeftKey].pressed
                && !keyArray[(int)numToKey.strafeRightKey].pressed
                && !keyArray[(int)numToKey.strafeUpKey].pressed
                && !keyArray[(int)numToKey.strafeDownKey].pressed)
            {

                PointF mouseMov = Game.Mouse.Movement;
                if (mouseMov.X != 0)
                {
                    camHeading -= mouseMov.X * mouseXmult;
                }
                if (mouseMov.Y != 0)
                {
                    camHeZ -= mouseMov.Y * mouseYmult;

                    if (camHeZ > 1.0f)
                    {
                        camHeZ = 1.0f;
                    }
                    else if (camHeZ < -1.0f)
                    {
                        camHeZ = -1.0f;
                    }
                }
            }

        }
        #endregion

        #region timebased events

        private void bombScript_Tick(object sender, EventArgs e)
        {
            if (customCamEnabled)
            {
                float fps = Game.FPS;
                float frameTime = 1f / fps; // time in seconds



                //GET THE CAM SPEED
                Vector3 tempCamPos = customCam.Position;
                Vector3 tempCamVel = new Vector3((tempCamPos.X - camOldPos.X) / frameTime, (tempCamPos.Y - camOldPos.Y) / frameTime, (tempCamPos.Z - camOldPos.Z) / frameTime);
                camOldPos = tempCamPos;
                float tempCamSpeed = (float)Math.Pow((tempCamVel.X * tempCamVel.X + tempCamVel.Y * tempCamVel.Y + tempCamVel.Z * tempCamVel.Z), 0.5f);
                //CAM SPEED CALCULATED!
#if DEBUG
                Game.Console.Print("test1");
                //Game.Console.Print(tempCamSpeed.ToString());
#endif
                if (useCameraObject)
                {
                    handleInputCamObj();
                }
                else
                {
                    handleInput();
                }
                updateCamInfo();
                Player.WantedLevel = 0;
                // Player.Character.Position = new Vector3(customCam.Position.X - 2 * camHeX, customCam.Position.Y - 2 * camHeY, customCam.Position.Z - 2 * camHeZ);
#if DEBUG
                //Game.Console.Print(GTA.Native.Function.Call<bool>("DOES_OBJECT_HAVE_PHYSICS", cameraObj).ToString());
                //GTA.Native.Function.Call("DOES_OBJECT_HAVE_PHYSICS", cameraObj, false);
                //Game.Console.Print("camObjXvel: " + camXvel.ToString() + " camObjZvel: " + camZvel + " real cam vel: " + cameraObj.Velocity.ToString());
#endif
            }
            else if (customCarCamEnabled)
            {
                if (Player.Character.isInVehicle())
                {
                    if (!isAdjustingCarCamPosition)
                    {
                        customCarCam.Position = Player.Character.CurrentVehicle.GetOffsetPosition(customCarCam_offsPos);
                        float tempZ = Player.Character.CurrentVehicle.Direction.Z + customCarCam_directionDiff.Z;
#if DEBUG
                        Game.Console.Print(Player.Character.CurrentVehicle.Rotation.Z + " tempZ" + tempZ + " p.Z " + Player.Character.Direction.Z.ToString() + " cCam.Z " + customCarCam.Direction.Z.ToString() + " diff " + customCarCam_directionDiff.Z);
#endif
                        
                        customCarCam.Direction = new Vector3(Player.Character.CurrentVehicle.Direction.X - customCarCam_directionDiff.X,
                                                             Player.Character.CurrentVehicle.Direction.Y - customCarCam_directionDiff.Y,
                                                             tempZ);
                        customCarCam.Heading = Player.Character.CurrentVehicle.Heading + customCarCam_headingDiff;
                        customCarCam.Rotation = new Vector3(tempZ, customCarCam.Rotation.Y, customCarCam.Rotation.Z);
                    }
                    else
                    {
                        Player.Character.CurrentVehicle.Speed = 0;
                        handleInput_car();
                        updateCarCamInfo();
                    }
#if USE_SOPHISTICATED_CAR_CAM
                    float tempX, tempY, tempZ,
                          temp_customCarCamHeading = customCarCam.Heading;
                    if (temp_customCarCamHeading < 0)
                    {
                        temp_customCarCamHeading += 360;
                    }
                    Vector3 tempPos = Player.Character.CurrentVehicle.Position,
                            tempPos2 = customCarCam.Position;
                    Vector3 tempVector = Player.Character.CurrentVehicle.GetOffsetPosition(customCarCam_aimCoord),
                            offsPos = Player.Character.CurrentVehicle.GetOffsetPosition(customCarCam_offsPos);


                    // METHOD: FLOATING CAM, NO HEADING ADJUSTMENT
                    tempX = tempPos.X - oldVehPos.X;
                    tempY = tempPos.Y - oldVehPos.Y;
                    tempZ = tempPos.Z - oldVehPos.Z;
                    oldVehPos = tempPos;                    
                    // END METHOD

                    Vector3 anotherTempVector = Player.Character.CurrentVehicle.GetOffset(customCarCam.Position);
#if DEBUG
                    Game.Console.Print(anotherTempVector.X.ToString() + "," + anotherTempVector.Y.ToString() + "," + anotherTempVector.Z.ToString());
                    //Game.Console.Print(customCarCam.Position.Z + " " + tempZ.ToString() + " " + oldVehPos.Z.ToString());
                   //Game.Console.Print("max X dev: " + camMaxXdeviation.ToString() + " max y dev: " + camMaxXdeviation.ToString() + " y-offs: " + customCarCam_offsPos.Y.ToString() + " offsetX: " + anotherTempVector.X.ToString());
                    //customCarCam.Position = new Vector3(tempPos2.X + tempX, tempPos2.Y + tempY, tempPos2.Z + tempZ);
#endif               
                    
                    if (anotherTempVector.X > 3.433099f)
                    {
                        Game.Console.Print("A");
                        Vector3 tempVector2 = Player.Character.CurrentVehicle.GetOffsetPosition(new Vector3(3.433099f, -7.092389f, 0));
                        customCarCam.Position = new Vector3(tempVector2.X, tempVector2.Y, Player.Character.CurrentVehicle.Position.Z + customCarCam_offsPos.Z);
                    }
                    else if (anotherTempVector.X < -3.433099f)
                    {
                        Game.Console.Print("B");
                        Vector3 tempVector2 = Player.Character.CurrentVehicle.GetOffsetPosition(new Vector3(-3.433099f, -7.092389f, 0));
                        customCarCam.Position = new Vector3(tempVector2.X, tempVector2.Y, Player.Character.CurrentVehicle.Position.Z + customCarCam_offsPos.Z);
                        //customCarCam.Position = Player.Character.CurrentVehicle.GetOffsetPosition(new Vector3(-2.0f, -(float)Math.Pow(55, 0.5), customCarCam.Position.Z));
                    }
                    else
                    {
                        customCarCam.Position = new Vector3(tempPos2.X + tempX, tempPos2.Y + tempY, tempPos2.Z + tempZ);
                    }
                    
                    customCarCam.Position = new Vector3(tempPos2.X + tempX, tempPos2.Y + tempY, tempPos2.Z + tempZ);
                    GTA.Native.Function.Call("POINT_CAM_AT_COORD", customCarCam, tempVector.X, tempVector.Y, Player.Character.CurrentVehicle.Position.Z + customCarCam_offsPos.Z);

                    // METHOD: HEADING ADJUSTMENT
                    /*
                    if (Player.Character.CurrentVehicle.Heading > temp_customCarCamHeading + maxHeadingDiff)
                    {
                        customCarCam.Position = Player.Character.GetOffsetPosition(new Vector3(offsPos.X - camMaxYdeviation, offsPos.Y, offsPos.Z));
                    }
                    else if (Player.Character.CurrentVehicle.Heading < temp_customCarCamHeading - maxHeadingDiff)
                    {
                        customCarCam.Position = Player.Character.GetOffsetPosition(new Vector3(offsPos.X + camMaxYdeviation, offsPos.Y, offsPos.Z));
                    }
                    else
                    {
                        customCarCam.Position = new Vector3(tempPos2.X + tempX, tempPos2.Y + tempY, tempPos2.Z + tempZ);
                    }
                    */
                    /*
                    if (headingAdjustBool)
                    {
                        if (tempPos2.DistanceTo(offsPos) < 1.5f)
                        {
                            headingAdjustBool = false;
                        }
                        // heading is too much, so we move to the offset pos:
                        float tempX2, tempY2, tempZ2, vectorLength;
                        tempX2 = offsPos.X - tempPos2.X; // get the WHOLE difference
                        tempY2 = offsPos.Y - tempPos2.Y;
                        tempZ2 = offsPos.Z - tempPos2.Z;

                        vectorLength = (float)(Math.Pow(tempX2, 2) + Math.Pow(tempY2, 2) + Math.Pow(tempZ2, 2));
                        tempX += (tempX2 / vectorLength) * headingReturnMult;
                        tempY += (tempY2 / vectorLength) * headingReturnMult;
                        tempZ += (tempZ2 / vectorLength) * headingReturnMult;
                    }
                    
                    // END METHOD

                    */                   
                    

#endif
                }
                else
                {
                    customCarCamEnabled = false;
                    customCarCam.isActive = false;
                }
            }
        }

        #endregion

        #region keyevents
        private void bombScript_KeyDown(object sender, GTA.KeyEventArgs e)
        {
            if (customCamEnabled || customCarCamEnabled)
            {
                for (int i = 0; i < numOfKeys; i++)
                {
                    if (e.Key == keyArray[i].key)
                    {
                        keyArray[i].pressed = true;
                    }
                }
            }
            if (customCarCamEnabled)
            {
                if (e.Key == keyArray[(int)numToKey.carCam_positionKey].key)
                {
                    isAdjustingCarCamPosition = !isAdjustingCarCamPosition;
                    if (!isAdjustingCarCamPosition)
                    {
                        customCarCam_offsPos = Player.Character.CurrentVehicle.GetOffset(customCarCam.Position);

                        customCarCam_headingDiff = customCarCam.Heading - Player.Character.CurrentVehicle.Heading;
                        customCarCam_directionDiff.X = -Player.Character.CurrentVehicle.Direction.X + customCarCam.Direction.X;
                        customCarCam_directionDiff.Y = -Player.Character.CurrentVehicle.Direction.Y + customCarCam.Direction.Y;
                        customCarCam_directionDiff.Z = -Player.Character.CurrentVehicle.Rotation.X + customCarCam.Rotation.X;
                        Game.Console.Print("pVeh.Z " + Player.Character.CurrentVehicle.Direction.Z.ToString() + " cCam.Z " + customCarCam.Direction.Z.ToString() + " diff " + customCarCam_directionDiff.Z);
                    }
                    else
                    {
                        camHeading = customCarCam.Heading;
                        camX = customCarCam.Position.X;
                        camY = customCarCam.Position.Y;
                        camZ = customCarCam.Position.Z;
                        camHeX = customCarCam.Direction.X;
                        camHeY = customCarCam.Direction.Y;
                        camHeZ = customCarCam.Direction.Z;
                    }

                }
            }
            if (e.Key == camKey)
            {
                if (!Player.Character.isInVehicle())
                {
                    customCamEnabled = !customCamEnabled;
                    if (customCamEnabled)
                    {
                        customCam.Position = Game.CurrentCamera.Position;
                        customCam.Direction = Game.CurrentCamera.Direction;
                        customCam.isActive = true;

                        camY = customCam.Position.Y;
                        camX = customCam.Position.X;
                        camZ = customCam.Position.Z;

                        camOldPos = customCam.Position;

                        camHeX = customCam.Direction.X;
                        camHeY = customCam.Direction.Y;
                        camHeZ = customCam.Direction.Z;

                        camHeading = customCam.Heading;
                        camFov = customCam.FOV;
                        camAngle = customCam.Roll;

                        Game.Console.Print("Custom cam enabled");
                        
                        
                        GTA.Native.Function.Call("SET_CHAR_COLLISION", Player.Character, 0);
                        GTA.Native.Function.Call("DISPLAY_RADAR", false);
                        Player.Character.Invincible = true;
                        Player.IgnoredByEveryone = true;
                        startPoint = Player.Character.Position;

                        GTA.Native.Function.Call("CAN_PHONE_BE_SEEN_ON_SCREEN", false);
                        GTA.Native.Function.Call("DISPLAY_HUD", false);
                        Player.Character.Visible = false;
                        if (useCameraObject)
                        {
#if PLAYER_IS_CAMERA

#endif

                            //GTA.Native.Function.Call("CREATE_OBJECT", "EC_BOMB", customCam.Position.X, customCam.Position.Y, customCam.Position.Z, test, 0);
                            
#if PLAYER_IS_CAMERA
                                GTA.Native.Function.Call("ATTACH_CAM_TO_PED", customCam, Player.Character);

                            if (Exists(cameraObj))
                            {
                             // GTA.Native.Function.Call("IS_OBJECT_STATIC", cameraObj, true);
                               //GTA.Native.Function.Call("SET_OBJECT_INITIAL_ROTATION_VELOCITY", cameraObj, 0.0f, 0.0f, 0.0f, 1);
                               // GTA.Native.Function.Call("SET_OBJECT_INITIAL_VELOCITY", cameraObj, 0.0f, 0.0f, 0.0f, 1);
                               //GTA.Native.Function.Call("SET_ACTIVATE_OBJECT_PHYSICS_AS_SOON_AS_IT_IS_UNFROZEN", cameraObj, false);
                               // GTA.Native.Function.Call("SET_OBJECT_PHYSICS_PARAMS", cameraObj, 0);
                                GTA.Native.Function.Call("ATTACH_CAM_TO_OBJECT", customCam, cameraObj);
                               // Player.Character.Position = Game.CurrentCamera.Position;
                               // GTA.Native.Function.Call("ATTACH_CAM_TO_PED", customCam, Player.Character);

                                customCam.DrunkEffectIntensity = 0.0f;
                                cameraObj.Collision = false;
                                cameraObj.Visible = false;
                                cameraObj.FreezePosition = false;
                                GTA.Native.Function.Call("SET_OBJECT_DYNAMIC", cameraObj, true);
                                GTA.Native.Pointer tempPointer = new GTA.Native.Pointer(typeof(float));
                                GTA.Native.Function.Call("GET_OBJECT_MASS", cameraObj, tempPointer);
                                cameraObjMass = tempPointer;
                                Game.Console.Print("Camera object spawned successfully");
                            }
                            else
                            {
                                useCameraObject = false;
                                Game.Console.Print("Failed to spawn camera-object");
                            }
#endif

                        }
                    }
                    else
                    {
                        GTA.Native.Function.Call("CAN_PHONE_BE_SEEN_ON_SCREEN", true);
                        GTA.Native.Function.Call("SET_CHAR_VISIBLE", Player.Character, true);
                        GTA.Native.Function.Call("SET_CHAR_COLLISION", Player.Character, 1);
                        GTA.Native.Function.Call("DISPLAY_HUD", true);
                        GTA.Native.Function.Call("DISPLAY_RADAR", true);
                        //Player.Character.Visible = true;
                        Player.Character.Invincible = false;
                        Player.IgnoredByEveryone = false;
                        Player.Character.Position = startPoint;
                        Player.Character.GravityMultiplier = 1.0f;

                        customCam.isActive = false;
                        Game.Console.Print("Custom cam OFF");
                    }
                }
                else // if the player is in a vehicle, we apply the car-cam
                {
                    customCarCamEnabled = !customCarCamEnabled;
                    if (customCarCamEnabled)
                    {

                        customCarCam.Direction = Game.CurrentCamera.Direction;
                        customCarCam.FOV = Game.CurrentCamera.FOV;

                        customCarCam.Position = Player.Character.CurrentVehicle.GetOffsetPosition(defaultCarCamPos);

                        oldVehPos = Player.Character.CurrentVehicle.Position;

                        camY = customCarCam.Position.Y;
                        camX = customCarCam.Position.X;
                        camZ = customCarCam.Position.Z;

                        camHeX = customCarCam.Direction.X;
                        camHeY = customCarCam.Direction.Y;
                        camHeZ = customCarCam.Direction.Z;

                        camHeading = customCarCam.Heading;
                        camFov = customCarCam.FOV;
                        camAngle = customCarCam.Roll;

                        GTA.Native.Function.Call("SET_CAM_MOTION_BLUR", customCarCam, true);
                        customCarCam.isActive = true;
                        Game.Console.Print("Custom car cam enabled");                        
                        camMaxXdeviation = (float)Math.Sin(toRadian(maxHeadingDiff) * toRadian(-customCarCam_offsPos.Y));
                        camMaxYdeviation = (float)Math.Pow(customCarCam_offsPos.Y * customCarCam_offsPos.Y - camMaxXdeviation * camMaxXdeviation, 0.5);

                        float tempHeading = customCarCam.Heading;
                        if (tempHeading < 0)
                        {
                            tempHeading += 360;
                        }
                        customCarCam_headingDiff = customCarCam.Heading - Player.Character.CurrentVehicle.Heading;
                        customCarCam.Direction = Player.Character.CurrentVehicle.Direction;
                        customCarCam_directionDiff.X = -Player.Character.CurrentVehicle.Direction.X + customCarCam.Direction.X;
                        customCarCam_directionDiff.Y = -Player.Character.CurrentVehicle.Direction.Y + customCarCam.Direction.Y;
                        customCarCam_directionDiff.Z = -Player.Character.CurrentVehicle.Rotation.X + customCarCam.Rotation.X;
                    }
                    else
                    {
                        customCarCam.isActive = false;
                        Game.Console.Print("Custom car cam OFF");
                    }
                }
            }
        }

        private float toRadian(float degree)
        {
            return (float)(degree * (Math.PI / 180));
        }

        private void cameraScript_KeyUp(object sender, GTA.KeyEventArgs e)
        {
            if (customCamEnabled || customCarCamEnabled)
            {
                for (int i = 0; i < numOfKeys; i++)
                {
                    if (e.Key == keyArray[i].key)
                    {
                        keyArray[i].pressed = false;
                    }
                }
            }
        }
        #endregion

        #region helper functions

        private void updateCarCamInfo()
        {                
            customCarCam.Position = new Vector3(camX, camY, camZ);
            customCarCam.Direction = new Vector3(camHeX, camHeY, camHeZ);
            customCarCam.Heading = camHeading;
            camHeX = customCarCam.Direction.X;
            camHeY = customCarCam.Direction.Y;
        }
        private void updateCamInfo()
        {
            if (useCameraObject)
            {
                //cameraObj.Position = new Vector3(camX, camY, camZ);
                customCam.Direction = new Vector3(camHeX, camHeY, camHeZ);
                customCam.Heading = camHeading;
                camHeX = customCam.Direction.X;
                camHeY = customCam.Direction.Y;
                camHeZ = customCam.Direction.Z;

                Vector3 tempCamObjVel = new Vector3(0, 0, 0);
                tempCamObjVel = new Vector3(camHeX * camForwardVel, camHeY * camForwardVel, camHeZ * camForwardVel);

                float tempFloat = camHeading - 180,
                      tempFloat2, tempFloat3;

                tempFloat2 = (float)Math.Cos((Math.PI / 180) * tempFloat) * camXvel;
                tempFloat3 = (float)Math.Sin((Math.PI / 180) * tempFloat) * camXvel;

                Player.Character.GravityMultiplier = 0.0f;
                tempCamObjVel = new Vector3(tempCamObjVel.X - tempFloat2, tempCamObjVel.Y - tempFloat3, tempCamObjVel.Z + camZvel);

#if PLAYER_IS_CAMERA
                if (!idlePosBool)
                {
                    Player.Character.Velocity = tempCamObjVel;
                }
                Player.Character.Visible = false;

#endif
            }
            else
            {
                customCam.Position = new Vector3(camX, camY, camZ);
                customCam.Direction = new Vector3(camHeX, camHeY, camHeZ);
                customCam.Heading = camHeading;
                camHeX = customCam.Direction.X;
                camHeY = customCam.Direction.Y;

                Player.Character.Position = new Vector3(customCam.Position.X - 2 * camHeX, customCam.Position.Y - 2 * camHeY, customCam.Position.Z - 2 * camHeZ);
            }
        }
        #endregion

    }
}
