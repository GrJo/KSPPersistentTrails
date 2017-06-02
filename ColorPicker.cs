using System;
using UnityEngine;

namespace PersistentTrails
{

    class ColorPicker : Window<ColorPicker>
    {

        private Texture2D colorTexture;
        private int ImageWidth = 234;
        private int ImageHeight = 199;

        private TrackEditWindow editWindow;

        public ColorPicker(TrackEditWindow editWindow) : base("Color Picker") {
            //Utilities.LoadTexture(ref colorTexture, "ColorPick.png");
            this.colorTexture = Utilities.LoadImage("ColorPick.png", ImageWidth, ImageHeight);
            this.editWindow = editWindow;
            
            //TODO force window width and height, disallow resize
            windowPos = new Rect(350, 30, 220, 120);
            SetResizeX(false);
            SetResizeY(false);
            SetSize(250, 250);
        }


        protected override void DrawWindowContents(int windowID)
        {
            //Debug.Log("colorPicker.doGUI");
            if (GUILayout.RepeatButton(/*new Rect(10, 10, ImageWidth, ImageHeight),*/ colorTexture))
            {
                
                Vector2 pickpos = Event.current.mousePosition;
                int aaa = Convert.ToInt32(pickpos.x);
                int bbb = Convert.ToInt32(pickpos.y);
                Color col = colorTexture.GetPixel(aaa, 41 - bbb);

                // "col" is the color value that Unity is returning.
                // Here you would do something with this color value, like
                // set a model's material tint value to this color to have it change
                // colors, etc, etc.
                //
                // Right now we are just printing the RGBA color values to the Console
                
                //Debug.Log("colorPicked! at " + pickpos.ToString() + ", color = " + pickedColor.ToString());
                editWindow.OnColorPicked(col);
                SetVisible(false);
            }
                

            //GUILayout.EndVertical();
        }
    }
}
