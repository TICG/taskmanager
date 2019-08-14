/*
 * SDRA API
 *
 * This API is for SDRA  It provides Source Document Assessment Data and Data Access 
 *
 * OpenAPI spec version: 1.0.0
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Runtime.Serialization;
using System.Text;

namespace DataServices.Models
{
    /// <summary>
    /// The unique identifier for the SDRA assessment document i.e. the sdoc_id
    /// </summary>
    [DataContract]
    public partial class SdocId : IEquatable<SdocId>
    { 
        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class SdocId {\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((SdocId)obj);
        }

        /// <summary>
        /// Returns true if SdocId instances are equal
        /// </summary>
        /// <param name="other">Instance of SdocId to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(SdocId other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return false;
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                // Suitable nullity checks etc, of course :)
                return hashCode;
            }
        }

        #region Operators
        #pragma warning disable 1591

        public static bool operator ==(SdocId left, SdocId right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SdocId left, SdocId right)
        {
            return !Equals(left, right);
        }

        #pragma warning restore 1591
        #endregion Operators
    }
}
