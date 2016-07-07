using TerrainEditor.Core.Services;
using TerrainEditor.UserControls.Urho3D;
using Urho;
using Urho.Actions;

namespace TerrainEditor.Viewmodels
{

    public class UrhoViewportController : Application , IUrho3DService
    {
        private Color m_backColor;

        public static Plane MainPlane { get; } = new Plane(Vector4.UnitZ);

        public Color BackColor
        {
            get { return m_backColor; }
            set
            {
                m_backColor = value;
                Renderer.GetViewport(0).SetClearColor(value);
            }
        }
        public Scene Scene { get; private set; }
        public Camera Camera { get; private set; }

        public UrhoViewportController(ApplicationOptions opts) : base(opts) { }

        protected override void Setup()
        {
            Log.LogMessage += args =>
            {
                System.Console.WriteLine(args.Message);
            };
            Input.MouseWheel += args =>
            {
                Camera.Node.RunActions(
                    new EaseOut(new MoveBy(0.5f, new Vector3(0, 0, args.Wheel * 0.6f)), 1));
            };
            Input.MouseMoved += args =>
            {
                if (args.Buttons == (int)MouseButton.Middle)
                {
                    Camera.Node.Position += new Vector3(args.DX * -0.02f, args.DY * 0.02f, 0);
                }
            };
            Input.KeyDown += args =>
            {
                switch (args.Key)
                {
                case Key.W:
                case Key.S:
                case Key.A:
                case Key.D:

                    var yValue = Input.GetKeyDown(Key.W) ? 1 : (Input.GetKeyDown(Key.S) ? -1 : 0);
                    var xValue = Input.GetKeyDown(Key.D) ? 1 : (Input.GetKeyDown(Key.A) ? -1 : 0);

                    Camera.Node.RunActions(
                        new EaseIn(new MoveBy(0.4f, new Vector3(xValue, yValue, 0)), 1));

                    break;
                }
            };
        }
        protected override void Start()
        {
            Renderer.DefaultZone.AmbientGradient = false;
            Renderer.DefaultZone.AmbientColor = Color.White;

            Scene = new Scene();
            Scene.CreateComponent<Octree>();
            Scene.CreateComponent<DebugRenderer>();

            var cameraNode = Scene.CreateChild(nameof(Camera));
            cameraNode.Position = new Vector3(0, 0, -10);
            Camera = cameraNode.CreateComponent<Camera>();
            Camera.Fov = 61;

            Scene.CreateChild(nameof(GridLines)).CreateComponent<GridLines>();

            Renderer.SetViewport(0, new Viewport(Scene, Camera, null));

            BackColor = new Color(37f/255, 37f/255, 37f/255);
        }
    }
}