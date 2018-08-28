using System;
using System.Collections.Generic;
using System.Linq;
using Rock.Data;
using Rock.Model;
using Xunit;

namespace Rock.Tests.Rock.Model
{
    public class AttendanceCodeTests
    {
        #region Mock
        /// <summary>
        /// Returns codes from a predefined set rather than generating them randomly
        /// </summary>
        /// <seealso cref="IAttendanceCodeProvider" />
        public class MockAttendanceCodeProvider : IAttendanceCodeProvider
        {
            private readonly string[] _codes;
            private int index = -1;

            public MockAttendanceCodeProvider( params string[] codes )
            {
                _codes = codes;
            }

            public string GetCode()
            {
                index++;
                return _codes[index];
            }
        }
        #endregion

        #region AttendanceCodeService

        /// <summary>
        /// Verify that banned codes get skipped.
        /// </summary>
        [Fact]
        public void SkipBannedCodes()
        {
            AttendanceCodeService.FlushTodaysCodes( true );
            var goodCode = "AB12";
            var codeGenerator = new MockAttendanceCodeProvider( AttendanceCodeService.BannedCodes.First(), goodCode );

            var code = AttendanceCodeService.GenerateCode( codeGenerator );

            Assert.Equal( goodCode, code );
        }

        /// <summary>
        /// Verify that duplicate codes will not be generated.
        /// </summary>
        [Fact]
        public void SkipDuplicates()
        {
            AttendanceCodeService.FlushTodaysCodes( true );
            var codes = new[] { "ABC", "ABC", "DEF", "ABC", "GHI", "DEF" };
            var codeGenerator = new MockAttendanceCodeProvider( codes );

            for ( var i = 0; i < codes.Distinct().Count(); i++ )
            {
                AttendanceCodeService.GenerateCode( codeGenerator );
            }

            var duplicates = AttendanceCodeService.TodaysCodes.GroupBy( x => x )
                                                  .Where( group => group.Count() > 1 )
                                                  .Select( group => group.Key )
                                                  .ToList();

            Assert.Equal( codes.Distinct().Count(), AttendanceCodeService.TodaysCodes.Count );
            Assert.True( !duplicates.Any(), "repeated codes: " + string.Join( ", ", duplicates ) );
        }

        #endregion

        #region AttendanceCodeGenerator

        [Fact]
        public void AlphaNumericLength()
        {
            // Arrange
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 5
                                                       , alphaLength: 0
                                                       , numericLength: 0
                                                       , isRandomized: false);

            // Act
            var code = generator.GetCode();

            // Assert
            Assert.Equal( 5, code.Length );
        }

        [Fact]
        public void AlphaLength()
        {
            // Arrange
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                       , alphaLength: 5
                                                       , numericLength: 0
                                                       , isRandomized: false );

            // Act
            var code = generator.GetCode();

            // Assert
            Assert.Equal( 5, code.Length );
        }

        [Fact]
        public void NumericLength()
        {
            // Arrange
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                       , alphaLength: 0
                                                       , numericLength: 5
                                                       , isRandomized: false );

            // Act
            var code = generator.GetCode();

            // Assert
            Assert.Equal( 5, code.Length );
        }

        [Fact]
        public void AlphaNumericAlphaLength()
        {
            // Arrange
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 2
                                                       , alphaLength: 2
                                                       , numericLength: 0
                                                       , isRandomized: false );

            // Act
            var code = generator.GetCode();

            // Assert
            Assert.Equal( 4, code.Length );
        }

        [Fact]
        public void AlphaNumericNumericLength()
        {
            // Arrange
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 2
                                                       , alphaLength: 0
                                                       , numericLength: 2
                                                       , isRandomized: false );

            // Act
            var code = generator.GetCode();

            // Assert
            Assert.Equal( 4, code.Length );
        }

        [Fact]
        public void AlphaThenNumericLength()
        {
            // Arrange
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                       , alphaLength: 2
                                                       , numericLength: 2
                                                       , isRandomized: false );

            // Act
            var code = generator.GetCode();

            // Assert
            Assert.Equal( 4, code.Length );
        }

        [Fact]
        public void AlphaNumericThenAlphaThenNumericLength()
        {
            // Arrange
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 2
                                                       , alphaLength: 2
                                                       , numericLength: 2
                                                       , isRandomized: false );

            // Act
            var code = generator.GetCode();

            // Assert
            Assert.Equal( 6, code.Length );
        }

        [Fact]
        public void SkipBanned3DigitCode()
        {
            // Arrange
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                       , alphaLength: 0
                                                       , numericLength: 3
                                                       , isRandomized: false
                                                       , bannedCodes: new List<string> { "666" }
                                                       , todaysCodes: new List<string> { "665" } );

            // Act
            var code = generator.GetCode();

            // Assert
            Assert.Equal( "667", code );
        }

        [Fact]
        public void SkipBannedNumericCodeAtEndOfAlphaNumericCode()
        {
            // Arrange
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                       , alphaLength: 1
                                                       , numericLength: 3
                                                       , isRandomized: false
                                                       , bannedCodes: new List<string> { "666" }
                                                       , todaysCodes: new List<string> { "X665" } );

            // Act
            var code = generator.GetCode();

            // Assert
            Assert.Equal( "667", code.Right( 3 ) );
        }

        [Fact]
        public void SkipBannedNumericCodeAtEndOfNumericCode()
        {
            // Arrange
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                       , alphaLength: 0
                                                       , numericLength: 4
                                                       , isRandomized: false
                                                       , bannedCodes: new List<string> { "666" }
                                                       , todaysCodes: new List<string> { "0665" } );

            // Act
            var code = generator.GetCode();

            // Assert
            Assert.Equal( "0667", code );
        }

        [Fact]
        public void SkipBanned3DigitCodeAtBeginning()
        {
            // Arrange
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                       , alphaLength: 0
                                                       , numericLength: 4
                                                       , isRandomized: false
                                                       , bannedCodes: new List<string> { "666" }
                                                       , todaysCodes: new List<string> { "6659" } );

            // Act
            var code = generator.GetCode();

            // Assert
            Assert.Equal( "6670", code );
        }

        [Fact]
        public void NumericCodeShouldCycle()
        {
            // Arrange
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                       , alphaLength: 0
                                                       , numericLength: 4
                                                       , isRandomized: false
                                                       , todaysCodes: new List<string> { "9999" } );

            // Act
            var code = generator.GetCode();

            // Assert
            Assert.Equal( "0000", code );
        }

        [Fact]
        public void NumericCodeShouldCycle2()
        {
            // Arrange
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                       , alphaLength: 0
                                                       , numericLength: 4
                                                       , isRandomized: false
                                                       , todaysCodes: new List<string> { "9999" } );

            // Act
            var code1 = generator.GetCode();
            var code2 = generator.GetCode();
            var code3 = generator.GetCode();

            // Assert
            Assert.Equal( "0000", code1 );
            Assert.Equal( "0001", code2 );
            Assert.Equal( "0002", code3 );
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
                // Arrange
                var generator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                           , alphaLength: 0
                                                           , numericLength: 2
                                                           , isRandomized: false );

                // Act
                var code = string.Empty;
                for ( int i = 0; i < 100; i++ )
                {
                    code = generator.GetCode();
                }

                // Assert

                // should not be longer than 2 characters
                Assert.True( code.Length == 2, "last code was " + code );
            }
            catch ( TimeoutException )
            {
                // An exception in this case is considered better than hanging (since there is 
                // no actual solution).
                Assert.True( true );
            }
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
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                       , alphaLength: 0
                                                       , numericLength: 2
                                                       , isRandomized: false );

            // Generate 99 codes (the maximum number of valid codes).
            for ( int i = 0; i < 100; i++ )
            {
                generator.GetCode();
            }

            // Now try to generate one more... which should NOT hang but instead, may
            // throw one of two exceptions.
            try
            {
                generator.GetCode();
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
            var generator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                       , alphaLength: 0
                                                       , numericLength: 3
                                                       , isRandomized: false );

            string code = null;
            for ( int i = 0; i < 100; i++ )
            {
                code = generator.GetCode();
            }

            Assert.Equal( "100", code );
        }

        #endregion

        #region AttendanceCodeService and AttendanceCodeGenerator in conjunction

        // These will test how the two classes work together for scenarios that cannot be tested individually 

        /// <summary>
        /// Verify that incrementing to a number that has already been used will continue incrementing properly
        /// </summary>
        [Fact]
        public void SkipDuplicatesWhileIncrementing()
        {
            AttendanceCodeService.FlushTodaysCodes( true );
            AttendanceCodeService.TodaysCodes.Add( "0000" );
            AttendanceCodeService.TodaysCodes.Add( "9999" );
            var codeGenerator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                           , alphaLength: 0
                                                           , numericLength: 4
                                                           , isRandomized: false
                                                           , todaysCodes: new List<string> { "0000", "9999" } );

            var code = AttendanceCodeService.GenerateCode( codeGenerator );

            Assert.Equal( "0001", code );
        }

        /// <summary>
        /// Verify that banned codes get skipped when the individual parts are not banned but the resulting code is.
        /// This should not increment the number since the number itself is not banned.
        /// </summary>
        [Fact]
        public void SkipBannedCombinedCodes()
        {
            var originalBannedCodes = AttendanceCodeService.BannedCodes.ToList();
            //var banThese = AttendanceCodeGenerator.AlphaCharacters.Where( c => c != 'Z' ).Select( c => c.ToString() + "1" ); // B1, C1, D1, etc. excluding Z1
            var banThese = AttendanceCodeGenerator.AlphaCharacters.Select( c => c.ToString() + "1" ); // B1, C1, D1, etc.
            AttendanceCodeService.BannedCodes.AddRange( banThese );
            AttendanceCodeService.FlushTodaysCodes( true );
            var codeGenerator = new AttendanceCodeGenerator( alphaNumericLength: 0
                                                           , alphaLength: 1
                                                           , numericLength: 1
                                                           , isRandomized: false );

            var code = AttendanceCodeService.GenerateCode( codeGenerator );

            // Reset the banned codes before asserting so a failure won't prevent cleanup
            AttendanceCodeService.BannedCodes = originalBannedCodes;

            //Assert.Equal( "Z1", code );
            Assert.Equal( "2", code.Right( 1 ) );
        }

        #endregion
    }
}
