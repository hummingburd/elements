using Hypar.Elements.Serialization;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Hypar.Geometry
{
    /// <summary>
    /// A coplanar continuous set of lines.
    /// </summary>
    public class Polyline: ICurve
    {
        /// <summary>
        /// The internal collection of vertices.
        /// </summary>
        protected IList<Vector3> _vertices;

        /// <summary>
        /// The vertices of the polygon.
        /// </summary>
        [JsonProperty("vertices"), JsonConverter(typeof(IListVector3Converter))]
        public IList<Vector3> Vertices
        {
            get{return this._vertices;}
        }

        /// <summary>
        /// The length of the polygon.
        /// </summary>
        [JsonIgnore]
        public double Length
        {
            get{return this.Segments().Sum(s=>s.Length);}
        }

        /// <summary>
        /// The start of the polyline.
        /// </summary>
        [JsonIgnore]
        public Vector3 Start
        {
            get{return this._vertices[0];}
        }

        /// <summary>
        /// The end of the polyline.
        /// </summary>
        [JsonIgnore]
        public Vector3 End
        {
            get{return this._vertices[this._vertices.Count - 1];}
        }

        /// <summary>
        /// Construct a polyline from a collection of vertices.
        /// </summary>
        /// <param name="vertices">A CCW wound set of vertices.</param>
        public Polyline(IList<Vector3> vertices)
        {
            for(var i=0; i<vertices.Count; i++)
            {
                for(var j=0; j<vertices.Count; j++)
                {
                    if(i == j)
                    {
                        continue;
                    }
                    if(vertices[i].IsAlmostEqualTo(vertices[j]))
                    {
                        throw new ArgumentException($"The polygon could not be constructed. Two vertices were almost equal: {i} {vertices[i]} {j} {vertices[j]}.");
                    }
                }
            }
            this._vertices = vertices;
        }

        /// <summary>
        /// Reverse the direction of a polyline.
        /// </summary>
        /// <returns>Returns a new polyline with opposite winding.</returns>
        public Polyline Reversed()
        {
            return new Polyline(this._vertices.Reverse().ToArray());
        }

        /// <summary>
        /// Get a string representation of this polyline.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Join(",", this._vertices);
        }

        /// <summary>
        /// Get a collection a lines representing each segment of this polyline.
        /// </summary>
        /// <returns>A collection of Lines.</returns>
        public virtual Line[] Segments()
        {
            var result = new Line[_vertices.Count-1];
            for (var i = 0; i < _vertices.Count-1; i++)
            {
                var a = _vertices[i];
                var b = _vertices[i+1];
                result[i] = new Line(a, b);
            }
            return result;
        }

        /// <summary>
        /// Get a point on the polygon at parameter u.
        /// </summary>
        /// <param name="u">A value between 0.0 and 1.0.</param>
        /// <returns>Returns a Vector3 indicating a point along the Polygon length from its start vertex.</returns>

        public Vector3 PointAt(double u)
        {
            var segmentIndex = 0;
            var p = PointAtInternal(u, out segmentIndex);
            return p;
        }

        /// <summary>
        /// Get a point on the polygon at parameter u.
        /// </summary>
        /// <param name="u">A value between 0.0 and 1.0.</param>
        /// <param name="segmentIndex">The index of the segment containing parameter u.</param>
        /// <returns>Returns a Vector3 indicating a point along the Polygon length from its start vertex.</returns>
        private Vector3 PointAtInternal(double u, out int segmentIndex)
        {
            if(u < 0.0 || u > 1.0)
            {
                throw new Exception($"The value of u ({u}) must be between 0.0 and 1.0.");
            }

            var d = this.Length * u;
            var totalLength = 0.0;
            for (var i = 0; i < this._vertices.Count - 1; i++)
            {
                var a = this._vertices[i];
                var b = this._vertices[i + 1];
                var currLength = a.DistanceTo(b);
                var currVec = (b - a).Normalized();
                if (totalLength <= d && totalLength + currLength >= d)
                {
                    segmentIndex = i;
                    return a + currVec * ((d - totalLength) / currLength);
                }
                totalLength += currLength;
            }
            segmentIndex = this._vertices.Count - 1;
            return this.End;
        }

        /// <summary>
        /// Get the Transform at the specified parameter along the Polygon.
        /// </summary>
        /// <param name="u">The parameter on the Polygon between 0.0 and 1.0.</param>
        /// <param name="up">An optional up vector for the Transform.</param>
        /// <returns>A Transform with its Z axis aligned trangent to the Polygon.</returns>
        public Transform TransformAt(double u, Vector3 up = null)
        {
            if(u < 0.0 || u > 1.0)
            {
                throw new ArgumentOutOfRangeException($"The provided value for u ({u}) must be between 0.0 and 1.0.");
            }
            
            var segmentIndex = 0;
            var o = PointAtInternal(u, out segmentIndex);
            var z = up != null ? up : Vector3.ZAxis;
            Vector3 x = null;

            // Check if the provided parameter is equal
            // to one of the vertices.
            var a = this.Vertices.FirstOrDefault(vtx=>vtx.Equals(o));
            if(o != null)
            {
                var idx = Vertices.IndexOf(o);
                Vector3 b;
                Vector3 c;
                if(idx == 0)
                {
                    b = this.Vertices[this.Vertices.Count-1];
                }
                else
                {
                    b = this.Vertices[idx-1];
                }
                c = this.Vertices[idx + 1];
                var v1 = (c-a).Normalized();
                var v2 = (b-a).Normalized();
                x = v1.Average(v2);
                return new Transform(o, x, z);
            }
            else
            {
                var d = this.Length * u;
                var totalLength = 0.0;
                var segments = Segments();
                for (var i = 0; i < segments.Length; i++)
                {
                    var s = segments[i];
                    var currLength = s.Length;
                    if (totalLength <= d && totalLength + currLength >= d)
                    {
                        o = s.PointAt((d - totalLength) / currLength);
                        x = s.Direction.Cross(z);
                        break;
                    }
                    totalLength += currLength;
                }
            }
            return new Transform(o, x, z);
        }

        /// <summary>
        /// Get the transforms used to transform a Profile extruded along this Polyline.
        /// </summary>
        /// <param name="startSetback"></param>
        /// <param name="endSetback"></param>
        /// <returns></returns>
        public virtual Transform[] Frames(double startSetback, double endSetback)
        {
            return FramesInternal(startSetback, endSetback);
        }

        internal Transform[] FramesInternal(double startSetback, double endSetback, bool closed=false)
        {
            var result = new Transform[this._vertices.Count];
            for(var i=0; i<result.Length; i++)
            {
                var a = this._vertices[i];
                if(closed)
                {
                    var b = i == 0 ? this._vertices[this._vertices.Count - 1] : this._vertices[i-1];
                    var c = i == this._vertices.Count - 1 ? this._vertices[0] : this._vertices[i+1];
                    var x = (b-a).Average(c-a);
                    result[i] = new Transform(this._vertices[i], x, x.Cross(Vector3.ZAxis));
                }
                else
                {
                    Vector3 b,c,x;

                    if(i == this.Vertices.Count - 1)
                    {
                        b = this._vertices[i-1];
                        x = (b-a).Normalized().Negated();
                        result[i] = new Transform(this._vertices[i], x.Cross(Vector3.ZAxis), x);
                    }
                    else if(i == 0)
                    {
                        b = this._vertices[i+1];
                        x = (b-a).Normalized();
                        result[i] = new Transform(this._vertices[i], x.Cross(Vector3.ZAxis), x);
                    }
                    else 
                    {
                        b = this._vertices[i-1];
                        c = this._vertices[i+1];
                        x = (b-a).Normalized().Average((c-a).Normalized());
                        result[i] = new Transform(this._vertices[i], x, x.Cross(Vector3.ZAxis));
                    }
                }
            }
            return result;
        }
    }
}