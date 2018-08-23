using System;
using System.Linq;
using Rock.Model;
using Xunit;

namespace Rock.Tests.Rock.Model
{
    public class AttendanceCodeTests
    {
        #region Alpha-numeric codes

        [Fact]
        public void SkipBanned3DigitCodeAtEndOfAlphaNumericCode()
        {
            int alphaNumericLength = 0;
            int alphaLength = 1;
            int numericLength = 3;
            bool isRandomized = false;
            string lastCode = "X665";

            string code = AttendanceCodeService.GetNextNumericCodeAsString( alphaNumericLength, alphaLength, numericLength, isRandomized, lastCode );
            Assert.Equal( "667", code.Right( 3 ) );
        }

        /// <summary>
        /// Verify that three character alpha-numeric codes are all good codes.
        /// </summary>
        [Fact]
        public void AlphaNumericCodesShouldSkipBadCodes()
        {
            AttendanceCodeService.FlushTodaysCodes(true);

            for ( int i = 0; i < 6000; i++ )
            {
                AttendanceCodeService.GetNewCode( 3, 0, 0, false );
            }

            bool hasMatchIsBad = AttendanceCodeService.TodaysCodes.Any( c => AttendanceCodeService.BannedCodes.Any( c.Contains ) );

            Assert.False( hasMatchIsBad );
        }

        #endregion

        #region Numeric only codes

        [Fact]
        public void SkipBanned3DigitCode()
        {
            int alphaNumericLength = 0;
            int alphaLength = 0;
            int numericLength = 3;
            bool isRandomized = false;
            string lastCode = "665";

            string code = AttendanceCodeService.GetNextNumericCodeAsString( alphaNumericLength, alphaLength, numericLength, isRandomized, lastCode );
            Assert.Equal( "667", code );
        }

        [Fact]
        public void SkipBanned3DigitCodeAtEnd()
        {
            int alphaNumericLength = 0;
            int alphaLength = 0;
            int numericLength = 4;
            bool isRandomized = false;
            string lastCode = "0665";

            string code = AttendanceCodeService.GetNextNumericCodeAsString( alphaNumericLength, alphaLength, numericLength, isRandomized, lastCode );
            Assert.Equal( "0667", code );
        }

        [Fact]
        public void SkipBanned3DigitCodeAtBeginning()
        {
            int alphaNumericLength = 0;
            int alphaLength = 0;
            int numericLength = 4;
            bool isRandomized = false;
            string lastCode = "6659";

            string code = AttendanceCodeService.GetNextNumericCodeAsString( alphaNumericLength, alphaLength, numericLength, isRandomized, lastCode );
            Assert.Equal( "6670", code );
        }

        /// <summary>
        /// Checks the three char "002" code.
        /// </summary>
        [Fact]
        public void CheckThreeChar002Code()
        {
            AttendanceCodeService.FlushTodaysCodes( true );

            string code = null;
            for ( int i = 0; i < 2; i++ )
            {
                code = AttendanceCodeService.GetNewCode( 0, 0, 3, false );
            }

            Assert.Equal( "002", code );
        }

        /// <summary>
        /// Confirms that the three restricted numeric codes
        /// </summary>
        [Fact]
        public void NumericCodesShouldNotContainRestrictedNumbers()
        {
            AttendanceCodeService.FlushTodaysCodes( true );

            for ( int i = 0; i < 2000; i++ )
            {
                AttendanceCodeService.GetNewCode( 0, 0, 4, false );
            }

            Assert.False( AttendanceCodeService.TodaysCodes.Any( s => s.Contains( "911" ) ) );
            Assert.False( AttendanceCodeService.TodaysCodes.Any( s => s.Contains( "666" ) ) );
            Assert.False( AttendanceCodeService.TodaysCodes.Any( s => s.Contains( "999" ) ) );
            Assert.False( AttendanceCodeService.TodaysCodes.Any( s => s.Contains( "455" ) ) );
        }

        /// <summary>
        /// Numeric only code with length of 2 should not go beyond 99.
        /// Attempting to create one should not be allowed so throwing a timeout
        /// exception is acceptable to let the administrator know there is a
        /// configuration problem.
        /// </summary>
        [Fact]
        public void NumericCodeWithLengthOf2ShouldNotGoBeyond99()
        {
            try
            {
                AttendanceCodeService.FlushTodaysCodes( true );

                for ( int i = 0; i < 101; i++ )
                {
                    AttendanceCodeService.GetNewCode( 0, 0, 2, false );
                }

                // should not be longer than 2 characters
                // This is a known bug in v7.4 and earlier, and possibly fixed via PR #3071
                var length = AttendanceCodeService.TodaysCodes.Last().Length;
                Assert.True( length == 2, "last code was " + length + " characters long." );
            }
            catch ( TimeoutException )
            {
                // An exception in this case is considered better than hanging (since there is 
                // no actual solution).
                Assert.True( true );
            }
        }

        /// <summary>
        /// Numerics codes should not repeat. There are 996 possible good numeric three character codes.
        /// </summary>
        [Fact]
        public void NumericCodesShouldNotRepeat()
        {
            AttendanceCodeService.FlushTodaysCodes( true );

            for ( int i = 0; i < 996; i++ )
            {
                AttendanceCodeService.GetNewCode( 0, 0, 3, false );
            }

            var duplicates = AttendanceCodeService.TodaysCodes.GroupBy( x => x )
                                                  .Where( group => group.Count() > 1 )
                                                  .Select( group => group.Key )
                                                  .ToList();

            Assert.True( !duplicates.Any(), "repeated codes: " + string.Join( ", ", duplicates ) );
        }

        /// <summary>
        /// Random numeric codes should not repeat. There are 996 possible good numeric three character codes.
        /// </summary>
        [Fact]
        public void RandomNumericCodesShouldNotRepeat()
        {
            AttendanceCodeService.FlushTodaysCodes( true );

            for ( int i = 0; i < 996; i++ )
            {
                AttendanceCodeService.GetNewCode( 0, 0, 3, true );
            }

            var duplicates = AttendanceCodeService.TodaysCodes.GroupBy( x => x )
                                                  .Where( group => group.Count() > 1 )
                                                  .Select( group => group.Key )
                                                  .ToList();

            Assert.True( !duplicates.Any(), "repeated codes: " + string.Join( ", ", duplicates ) );
        }

        /// <summary>
        /// Requestings the more codes than are possible should throw exception...
        /// because there's really nothing else we could do in that situation, right?
        /// 
        /// NOTE: This test has a special setup using an async task so that we can break
        /// out if the underlying Rock service call is hung in an infinite loop.
        /// </summary>
        [Fact]
        public void RequestingMoreCodesThanPossibleShouldThrowException()
        {
            AttendanceCodeService.FlushTodaysCodes( true );

            // Generate 99 codes (the maximum number of valid codes).
            for ( int i = 0; i < 100; i++ )
            {
                AttendanceCodeService.GetNewCode( 0, 0, 2, true );
            }

            // Now try to generate one more... which should NOT hang but instead, may
            // throw one of two exceptions.
            try
            {
                AttendanceCodeService.GetNewCode( 0, 0, 2, true );
            }
            catch ( InvalidOperationException )
            {
                Assert.True( true );
            }
            catch ( TimeoutException )
            {
                // An exception in this case is considered better than hanging (since there is 
                // no actual solution).
                Assert.True( true );
            }
        }

        /// <summary>
        /// Sequentially increment three-character numeric codes to 100 and verify "100".
        /// </summary>
        [Fact]
        public void Increment100SequentialNumericCodes()
        {
            AttendanceCodeService.FlushTodaysCodes( true );

            string code = null;
            for ( int i = 0; i < 100; i++ )
            {
                code = AttendanceCodeService.GetNewCode( 0, 0, 3, false );
            }

            Assert.Equal( "100", code );
        }

        #endregion

        #region Alpha only codes

        /// <summary>
        /// Three character alpha only codes should not 'contain' any bad codes.
        /// </summary>
        [Fact]
        public void AlphaOnlyCodesShouldSkipBadCodes()
        {
            AttendanceCodeService.FlushTodaysCodes( true );

            for ( int i = 0; i < 1000; i++ )
            {
                AttendanceCodeService.GetNewCode( 0, 3, 0, true );
            }

            bool hasMatchIsBad = AttendanceCodeService.TodaysCodes.Any( c => AttendanceCodeService.BannedCodes.Any( c.Contains ) );

            Assert.False( hasMatchIsBad );
        }

        /// <summary>
        /// Alpha codes should not repeat.  For three character codes there are approximately
        ///  4847 (17*17*17 minus ~50 bad codes) possible combinations of the 17 letters
        /// </summary>
        [Fact]
        public void AlphaOnlyCodesShouldNotRepeat()
        {
            AttendanceCodeService.FlushTodaysCodes( true );

            // 4847 (17*17*17 minus ~50 bad codes) possible combinations of 17 letters
            for ( int i = 0; i < 4860; i++ )
            {
                //System.Diagnostics.Debug.WriteIf( i > 4700, "code number " + i + " took... " );
                AttendanceCodeService.GetNewCode( 0, 3, 0, false );
                
                //System.Diagnostics.Debug.WriteLineIf( i > 4700, "" );
            }

            var duplicates = AttendanceCodeService.TodaysCodes.GroupBy( x => x )
                                    .Where( group => group.Count() > 1 )
                                    .Select( group => group.Key );

            Assert.True( !duplicates.Any() );
        }

        #endregion

        #region Alpha-numeric + numeric only codes

        /// <summary>
        /// Two character alpha numeric codes (AttendanceCodeService.codeCharacters) has possible
        /// 24*24 (576) combinations plus two character numeric codes has a possible 10*10 (100)
        /// for a total set of 676 combinations.  Removing the noGood (~60) codes leaves us with
        /// a valid set of about 616 codes.
        /// 
        /// NOTE: This appears to be a possible bug in v8.0 and earlier. The AttendanceCodeService
        /// service will only generate 100 codes when trying to combine the numeric parameter of "2" with
        /// the other parameters.
        ///
        /// Even when run with 2 alpha numeric and 3 numeric, this test should verify that codes
        /// such as X6662, 99119, 66600 do not occur.
        /// 
        /// There should be no bad codes in the generated AttendanceCodeService.TodaysCodes -- even though
        /// individually each part has no bad codes.  For example, "A6" + "66" should
        /// not appear since combined it would be "A666".
        /// </summary>
        [Fact]
        public void AlphaNumericWithNumericCodesShouldSkipBadCodes()
        {
            AttendanceCodeService.FlushTodaysCodes( true );

            int attemptCombination = 0;

            try
            {
                for ( int i = 0; i < 600; i++ )
                {
                    attemptCombination = i;
                    AttendanceCodeService.GetNewCode( 2, 0, 3, true );
                }

                var matches = AttendanceCodeService.TodaysCodes.Where( c => AttendanceCodeService.BannedCodes.Any( c.Contains ) ).ToList();
                bool hasMatchIsBad = matches.Any();

                Assert.False( hasMatchIsBad, "bad codes were: " + string.Join( ", ", matches ) );
            }
            catch ( TimeoutException )
            {
                // If an infinite loop was detected, but we tried at least 600 codes then
                // we'll consider this a pass.
                Assert.True( attemptCombination >= 600 );
            }
        }

        #endregion

        #region Alpha only + numeric only codes

        /// <summary>
        /// This is the configuration that churches like Central Christian Church use for thier
        /// Children's check-in.
        /// </summary>
        [Fact]
        public void TwoAlphaWithFourRandomNumericCodesShouldSkipBadCodes()
        {
            AttendanceCodeService.FlushTodaysCodes( true );

            for ( int i = 0; i < 2500; i++ )
            {
                AttendanceCodeService.GetNewCode( 0, 2, 4, true );
            }

            var matches = AttendanceCodeService.TodaysCodes.Where( c => AttendanceCodeService.BannedCodes.Any( ng => c.Contains( ng ) ) ).ToList();

            bool hasMatchIsBad = matches.Any();

            Assert.False( hasMatchIsBad, "bad codes were: " + string.Join( ", ", matches ) );
        }

        /// <summary>
        /// Codes containing parts combined into noGood codes, such as "P" + "55",
        /// should not occur.
        /// </summary>
        [Fact]
        public void AlphaOnlyWithNumericOnlyCodesShouldSkipBadCodes()
        {
            AttendanceCodeService.FlushTodaysCodes( true );

            AttendanceCode code = null;
            for ( int i = 0; i < 6000; i++ )
            {
                AttendanceCodeService.GetNewCode( 0, 1, 4, true );
            }

            var matches = AttendanceCodeService.TodaysCodes.Where( c => AttendanceCodeService.BannedCodes.Any( ng => c.Contains( ng ) ) ).ToList();
            bool hasMatchIsBad = matches.Any();

            Assert.False( hasMatchIsBad, "bad codes were: " + string.Join( ", ", matches ) );
        }

        #endregion
    }
}
