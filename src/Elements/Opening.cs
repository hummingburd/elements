using System;
using Elements.Geometry;
using Elements.Geometry.Interfaces;
using Elements.Geometry.Solids;
using Newtonsoft.Json;

namespace Elements
{
    /// <summary>
    /// A rectangular opening in a wall or floor.
    /// </summary>
    public class Opening
    {
        private Polygon _perimeter;

        /// <summary>
        /// The name of the opening.
        /// </summary>
        [JsonProperty("name")]
        public string Name{get; internal set;}

        /// <summary>
        /// The perimeter of the opening.
        /// </summary>
        /// <value>A polygon of Width and Height translated by X and Y.</value>
        [JsonProperty("perimeter")]
        public Polygon Perimeter 
        {
            get => _perimeter;
            internal set => _perimeter = value;
        }

        /// <summary>
        /// Create an opening.
        /// </summary>
        /// <param name="x">The distance along the X axis of the transform of the host element to the center of the opening.</param>
        /// <param name="y">The distance along the Y axis of the transform of the host element to the center of the opening.</param>
        /// <param name="width">The width of the opening.</param>
        /// <param name="height">The height of the opening.</param>
        public Opening(double x, double y, double width, double height)
        {
            this._perimeter = Polygon.Rectangle(width, height, new Vector3(x, y));
        }

        /// <summary>
        /// Create an opening.
        /// </summary>
        /// <param name="perimeter">A polygon representing the perimeter of the opening.</param>
        /// <param name="x">The distance along the X axis of the transform of the host element to transform the perimeter.</param>
        /// <param name="y">The distance along the Y axis of the transform of the host element to transform the perimeter.</param>
        [JsonConstructor]
        public Opening(Polygon perimeter, double x = 0.0, double y = 0.0)
        {
            // this.X = x;
            // this.Y = y;
            var t = new Transform(x,y,0.0);
            this._perimeter = t.OfPolygon(perimeter);
        }
    }
}