using System;

namespace Checkers
{
    public class OnStepEventArgs : EventArgs
    {
        public readonly ColorType Side;
        public readonly Tuple<int, int> From;
        public readonly Tuple<int, int> To;

        public OnStepEventArgs(ColorType side, Tuple<int, int> from, Tuple<int, int> to)
        {
            Side = side;
            From = from;
            To = to;
        }

        public OnStepEventArgs(string serializedString) 
        {
            var items = serializedString.Split('/');
            Side = (ColorType)byte.Parse(items[0]);
            var fromCoords = items[1].Split(',');
            var toCoords = items[2].Split(',');
            From = new Tuple<int, int>(int.Parse(fromCoords[0]), int.Parse(fromCoords[1]));
            To = new Tuple<int, int>(int.Parse(toCoords[0]), int.Parse(toCoords[1]));
        }

        public string ToSerializedString() => $"{(byte)Side}/{From.Item1},{From.Item2}/{To.Item1},{To.Item2}";
    }

    public class CellCoordsEventArgs : EventArgs
    {
        public readonly int x;
        public readonly int z;

        public CellCoordsEventArgs(int x, int z)
        {
            this.x = x;
            this.z = z;
        }
    }

    public delegate void OnStepEventHandler(object sender, OnStepEventArgs args);
    
    public delegate void OnEatChipEventHandler(object sender, CellCoordsEventArgs args);

    public delegate void OnSelectChipEventHandler(object sender, CellCoordsEventArgs args);
}