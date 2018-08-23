// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using Rock.Data;

namespace Rock.Model
{
    /// <summary>
    /// Data Access/Service class for <see cref="Rock.Model.AttendanceCode"/> entity types
    /// </summary>
    public partial class AttendanceCodeService
    {
        private static readonly object _obj = new object();
        private static readonly Random _random = new Random( Guid.NewGuid().GetHashCode() );
        private static DateTime? _today;

        /// <summary>
        /// An array of alpha characters that can be used as a part of  <see cref="Rock.Model.AttendanceCode">AttendanceCodes</see>
        /// </summary>
        public static readonly char[] AlphaCharacters = { 'B', 'C', 'D', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'R', 'S', 'T', 'X', 'Z' };

        /// <summary>
        /// An array of numeric characters that can be used as a part of  <see cref="Rock.Model.AttendanceCode">AttendanceCodes</see>
        /// </summary>
        public static readonly char[] NumericCharacters = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        /// <summary>
        /// An array of characters that can be used as a part of  <see cref="Rock.Model.AttendanceCode">AttendanceCodes</see>
        /// </summary>
        public static readonly char[] CodeCharacters = AlphaCharacters.Concat( NumericCharacters ).ToArray();

        /// <summary>
        /// A list of <see cref="System.String"/> values that are not allowable as attendance codes.
        /// </summary>
        public static readonly List<string> BannedCodes = new List<string> {
            "4NL", "4SS", "455", "5CK", "5HT", "5LT", "5NM", "5TD", "5XX", "666", "BCH", "CLT", "CNT", "D4M", "D5H", "DCK", "DMN", "DSH", "F4G", "FCK", "FGT", "G4Y", "GZZ", "H8R",
            "JNK", "JZZ", "KKK", "KLT", "KNT", "L5D", "LCK", "LSD", "MFF", "MLF", "ND5", "NDS", "NDZ", "NGR", "P55", "PCP", "PHC", "PHK", "PHQ", "PM5", "PMS", "PN5", "PNS",
            "PRC", "PRK", "PRN", "PRQ", "PSS", "RCK", "SCK", "S3X", "SHT", "SLT", "SNM", "STD", "SXX", "THC", "V4G", "WCK", "XTC", "XXX", "911", "999" };

        public static HashSet<string> TodaysCodes { get; private set; }

        /// <summary>
        /// Returns a new <see cref="Rock.Model.AttendanceCode"/> comprised of random alpha numeric characters.
        /// </summary>
        /// <param name="codeLength">A <see cref="System.Int32"/> representing the length of the (security) code.</param>
        /// <returns>A new <see cref="Rock.Model.AttendanceCode"/></returns>
        public static AttendanceCode GetNew( int codeLength = 3 )
        {
            return GetNew( codeLength, 0, 0, false );
        }

        /// <summary>
        /// Returns a new <see cref="Rock.Model.AttendanceCode" /> with a specified number of alpha characters followed by
        /// another specified number of numeric characters.  The numeric character sequence will not repeat for "today" so 
        /// ensure that you're using a sufficient numericLength otherwise it will be unable to find a unique number.
        /// Also note as the issued numeric codes reaches the maximum (from the set of possible), it will take longer and
        /// longer to find an unused number.
        /// </summary>
        /// <param name="alphaLength">A <see cref="System.Int32"/> representing the length of the (alpha) portion of the code.</param>
        /// <param name="numericLength">A <see cref="System.Int32"/> representing the length of the (digit) portion of the code.</param>
        /// <param name="isRandomized">A <see cref="System.Boolean"/> that controls whether or not the AttendanceCodes should be generated randomly or in order (starting from the smallest).</param>
        /// <returns>
        /// A new <see cref="Rock.Model.AttendanceCode" />
        /// </returns>
        public static AttendanceCode GetNew( int alphaLength = 2, int numericLength = 4, bool isRandomized = true )
        {
            return GetNew( 0, alphaLength, numericLength, isRandomized );
        }

        /// <summary>
        /// Gets the new.
        /// </summary>
        /// <param name="alphaNumericLength">Length of the alpha numeric.</param>
        /// <param name="alphaLength">Length of the alpha.</param>
        /// <param name="numericLength">Length of the numeric.</param>
        /// <param name="isRandomized">if set to <c>true</c> [is randomized].</param>
        /// <returns></returns>
        public static AttendanceCode GetNew( int alphaNumericLength, int alphaLength, int numericLength, bool isRandomized )
        {
            lock ( _obj )
            {
                using ( var rockContext = new RockContext() )
                {
                    var service = new AttendanceCodeService( rockContext );

                    var today = RockDateTime.Today;
                    if ( TodaysCodes == null || !_today.HasValue || !_today.Value.Equals( today ) )
                    {
                        _today = today;
                        var tomorrow = today.AddDays( 1 );
                        TodaysCodes = new HashSet<string>( service.Queryable().AsNoTracking()
                            .Where( c => c.IssueDateTime >= today && c.IssueDateTime < tomorrow )
                            .Select( c => c.Code )
                            .ToList() );
                    }

                    var code = GetNewCode( alphaNumericLength, alphaLength, numericLength, isRandomized );

                    var attendanceCode = new AttendanceCode
                    {
                        IssueDateTime = RockDateTime.Now,
                        Code = code
                    };
                    service.Add( attendanceCode );
                    rockContext.SaveChanges();

                    return attendanceCode;
                }
            }
        }

        public static string GetNewCode( int alphaNumericLength, int alphaLength, int numericLength, bool isRandomized )
        {
            var lastCode = string.Empty;

            if ( numericLength > 0 && !isRandomized )
            {
                lastCode = TodaysCodes.Where( c => c.Length == alphaNumericLength + alphaLength + numericLength )
                                      .OrderBy( c => c.Substring( alphaNumericLength + alphaLength ) )
                                      .LastOrDefault();
            }

            var attempts = 0;
            var code = string.Empty;

            while ( code.Length == 0 || BannedCodes.Any( s => code.Contains( s ) ) || TodaysCodes.Contains( code ) )
            {
                attempts++;

                // We're only going to attempt this 1 million times...
                // Interestingly, even when this code approaches the maximum number of possible combinations
                // it still typically takes less than 5000 attempts. However, if the number of
                // attempts jumps over 10,000 there is almost certainly a problem with someone's
                // check-in code configuration so we're going to stop after a million attempts.
                if ( attempts > 1000000 )
                {
                    throw new TimeoutException( "Too many attempts to create a unique attendance code.  There is almost certainly a check-in system 'Security Code Length' configuration problem." );
                }

                code = GenerateRandomCode( alphaNumericLength )
                       + GenerateRandomAlphaCode( alphaLength )
                       + GetNextNumericCodeAsString( alphaNumericLength, alphaLength, numericLength, isRandomized, lastCode );
            }

            TodaysCodes.Add( code );
            return code;
        }

        /// <summary>
        /// Gets the next numeric code as string.
        /// </summary>
        /// <param name="alphaNumericLength">Length of the alpha numeric.</param>
        /// <param name="alphaLength">Length of the alpha.</param>
        /// <param name="numericLength">Length of the numeric.</param>
        /// <param name="isRandomized">if set to <c>true</c> [is randomized].</param>
        /// <param name="lastCode">The last code.</param>
        /// <returns></returns>
        public static string GetNextNumericCodeAsString( int alphaNumericLength, int alphaLength, int numericLength, bool isRandomized, string lastCode )
        {
            if ( isRandomized )
            {
                return GenerateRandomNumericCode( numericLength );
            }

            if ( !string.IsNullOrEmpty( lastCode ) )
            {
                var maxCode = lastCode.Substring( alphaNumericLength + alphaLength );
                int nextCode = maxCode.AsInteger() + 1;

                // Cycle through any numbers that include any restricted numbers until an unrestricted one is found
                while ( BannedCodes.Any( s => nextCode.ToString().Contains( s ) ) )
                {
                    nextCode += 1;
                }

                return nextCode.ToString( "D" + numericLength );
            }
            return 1.ToString( "D" + numericLength );
        }

        /// <summary>
        /// Generates a random (security) code.
        /// </summary>
        /// <param name="length">A <see cref="System.Int32"/> representing the length that the code needs to be.</param>
        /// <returns>A <see cref="System.String"/> representing the (security) code.</returns>
        private static string GenerateRandomCode( int length )
        {
            StringBuilder sb = new StringBuilder();

            int poolSize = CodeCharacters.Length;
            for ( int i = 0; i < length; i++ )
            {
                sb.Append( CodeCharacters[_random.Next( poolSize )] );
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a random (security) code containing only alpha characters.
        /// </summary>
        /// <param name="length">A <see cref="System.Int32"/> representing the length that the code needs to be.</param>
        /// <returns>A <see cref="System.String"/> representing the (security) code.</returns>
        private static string GenerateRandomAlphaCode( int length )
        {
            StringBuilder sb = new StringBuilder();

            int poolSize = AlphaCharacters.Length;
            for ( int i = 0; i < length; i++ )
            {
                sb.Append( AlphaCharacters[_random.Next( poolSize )] );
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a random (security) code containing only numeric characters.
        /// </summary>
        /// <param name="length">A <see cref="System.Int32"/> representing the length that the code needs to be.</param>
        /// <returns>A <see cref="System.String"/> representing the (security) code.</returns>
        private static string GenerateRandomNumericCode( int length )
        {
            StringBuilder sb = new StringBuilder();

            int poolSize = NumericCharacters.Length;
            for ( int i = 0; i < length; i++ )
            {
                sb.Append( NumericCharacters[_random.Next( poolSize )] );
            }

            return sb.ToString();
        }

        /// <summary>
        /// Clears the list of codes generated today so far.
        /// </summary>
        /// <param name="testing">If set to <c>true</c>, the list will not repopulate from the database before new codes are added.
        /// This will allow an integration test to ignore codes that it did not generate.</param>
        public static void FlushTodaysCodes(bool testing = false)
        {
            lock ( _obj )
            {
                if ( testing )
                {
                    TodaysCodes = new HashSet<string>();
                    _today = RockDateTime.Today;
                }
                else
                {
                    TodaysCodes = null;
                }
            }
        }
    }
}
