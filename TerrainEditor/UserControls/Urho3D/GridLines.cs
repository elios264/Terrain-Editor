using Urho;

namespace TerrainEditor.UserControls.Urho3D
{
    public class GridLines : CustomGeometry
    {
        private Vector2 m_size = new Vector2(500,500);
        private float m_majorSplit = 6;
        private float m_minorSplit = 2f;

        public Vector2 Size
        {
            get { return m_size; }
            set
            {
                m_size = value;
                Tesellate();
            }
        }
        public float MajorSplit
        {
            get { return m_majorSplit; }
            set
            {
                m_majorSplit = value;
                Tesellate();
            }
        }
        public float MinorSplit
        {
            get { return m_minorSplit; }
            set
            {
                m_minorSplit = value; 
                Tesellate();
            }
        }

        public GridLines()
        {
            ReceiveSceneUpdates = false;
        }
        public void Tesellate()
        {
            var gridMat = new Material();
            gridMat.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitVCol, 1, 1);
            SetMaterial(gridMat);
            Occludee = false;

            BeginGeometry(0, PrimitiveType.LineList);

            var halfSize = Size/2;
            //rows
            for (var y = halfSize.Y; y >= -halfSize.Y; y -= MinorSplit)
            {
                var color = y == 0 ? Color.Green : (y%MajorSplit == 0 ? Color.White : Color.Gray);

                DefineVertex(new Vector3(-halfSize.X, y, 0));
                DefineColor(color);
                DefineVertex(new Vector3(halfSize.X, y, 0));
                DefineColor(color);
            }
            //columns
            for (var x = -halfSize.X; x <= halfSize.X; x += MinorSplit)
            {
                var color = x == 0 ? Color.Red : (x%MajorSplit == 0 ? Color.White : Color.Gray);

                DefineVertex(new Vector3(x, -halfSize.Y, 0));
                DefineColor(color);
                DefineVertex(new Vector3(x, halfSize.Y, 0));
                DefineColor(color);
            }
            Commit();
        }

        public override void OnAttachedToNode(Node node)
        {
            base.OnAttachedToNode(node);
            Tesellate();
        }
    }
}