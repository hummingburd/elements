using Elements.Geometry;
using Elements.Geometry.Interfaces;
using Elements.Geometry.Profiles;
using Elements.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Elements.Geometry.Solids;

namespace Elements
{
    /// <summary>
    /// A boundary of an occupiable region.
    /// </summary>
    public class Space : Element, IGeometry3D, IProfileProvider
    {
        /// <summary>
        /// The profile of the space.
        /// </summary>
        [JsonProperty("profile")]
        public Profile Profile { get; }

        /// <summary>
        /// The transformed profile of the space.
        /// </summary>
        [JsonIgnore]
        public Profile ProfileTransformed
        {
            get { return this.Transform != null ? this.Transform.OfProfile(this.Profile) : this.Profile; }
        }

        /// <summary>
        /// The space's geometry.
        /// </summary>
        [JsonProperty("geometry")]
        public Solid[] Geometry { get; internal set; }

        /// <summary>
        /// Construct a space from a solid.
        /// </summary>
        /// <param name="geometry">The BRep which will be used to define the space.</param>
        /// <param name="transform">The Transform of the space.</param>
        public Space(Solid geometry, Transform transform = null)
        {
            this.Transform = transform;
            this.Geometry = new[] { geometry };
        }

        /// <summary>
        /// Construct a space.
        /// </summary>
        /// <param name="profile">The profile of the space.</param>
        /// <param name="height">The height of the space.</param>
        /// <param name="elevation">The elevation of the space.</param>
        /// <param name="material">The space's material.</param>
        /// <param name="transform">The space's transform.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the height is less than or equal to 0.0.</exception>
        public Space(Profile profile, double height, double elevation = 0.0, Material material = null, Transform transform = null)
        {
            if (height <= 0.0)
            {
                throw new ArgumentOutOfRangeException($"The Space could not be created. The height provided, {height}, was less than zero. The height must be greater than zero.", "height");
            }

            this.Transform = transform != null ? transform : new Transform(new Vector3(0, 0, elevation));
            this.Geometry = new[] { Solid.SweepFace(profile.Perimeter, profile.Voids, height, material == null ? BuiltInMaterials.Default : material) };
        }

        /// <summary>
        /// Construct a Space.
        /// </summary>
        /// <param name="profile">The profile of the space.</param>
        /// <param name="height">The height of the space above the lower elevation.</param>
        /// <param name="elevation">The elevation of the space.</param>
        /// <param name="material">The space's material.</param>
        /// <param name="transform">The space's transform.</param>
        public Space(Polygon profile, double height, double elevation = 0.0, Material material = null, Transform transform = null)
        {
            if (height <= 0.0)
            {
                throw new ArgumentOutOfRangeException($"The Space could not be created. The height provided, {height}, was less than zero. The height must be greater than zero.", "height");
            }

            this.Profile = new Profile(profile);
            this.Transform = transform != null ? transform : new Transform(new Vector3(0, 0, elevation));
            this.Geometry = new[] { Solid.SweepFace(this.Profile.Perimeter, this.Profile.Voids, height, material == null ? BuiltInMaterials.Mass : material) };
        }

        /// <summary>
        /// Construct a space from an array of solids.
        /// </summary>
        /// <param name="geometry">An array of solids which will be used to define the space.</param>
        /// <param name="transform">The space's Transform.</param>
        /// <param name="material">The space's Material.</param>
        [JsonConstructor]
        public Space(Solid[] geometry, Transform transform = null, Material material = null)
        {
            if (geometry == null || geometry.Length == 0)
            {
                throw new ArgumentOutOfRangeException("You must supply at least one IBRep to construct a Space.");
            }

            // TODO: Remove this when the Profile is no longer available
            // as a property on the Element. 
            // foreach(var g in geometry)
            // {
            //     var extrude = g as Extrude;
            //     if(extrude != null)
            //     {
            //         this.Profile = extrude.Profile;
            //     }
            // }

            this.Transform = transform;
            this.Geometry = geometry;
        }
    }
}