using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NUnit.Framework;
using PurplePen;
using PurplePen.ViewModels;

namespace PurplePenViewModels.Tests
{
    /// <summary>
    /// Tests for AddCourseDialogViewModel, verifying property defaults,
    /// computed properties, validation, and course kind switching behavior.
    /// </summary>
    [TestFixture]
    public class AddCourseDialogViewModelTests
    {
        private AddCourseDialogViewModel vm = null!;

        [SetUp]
        public void Initialize()
        {
            vm = new AddCourseDialogViewModel();
        }

        // ===== Default values =====

        /// <summary>
        /// Course name should default to empty string.
        /// </summary>
        [Test]
        public void CourseName_DefaultsToEmpty()
        {
            Assert.That(vm.CourseName, Is.EqualTo(""));
        }

        /// <summary>
        /// Course kind should default to Normal.
        /// </summary>
        [Test]
        public void CourseKind_DefaultsToNormal()
        {
            Assert.That(vm.CourseKind, Is.EqualTo(CourseKind.Normal));
        }

        /// <summary>
        /// CanChangeCourseKind should default to true (new course).
        /// </summary>
        [Test]
        public void CanChangeCourseKind_DefaultsToTrue()
        {
            Assert.That(vm.CanChangeCourseKind, Is.True);
        }

        /// <summary>
        /// Print scale text should default to "10000" from the designer constructor.
        /// </summary>
        [Test]
        public void PrintScaleText_DefaultsTo10000()
        {
            Assert.That(vm.PrintScaleText, Is.EqualTo("10000"));
        }

        /// <summary>
        /// Constructor should populate default print scales.
        /// </summary>
        [Test]
        public void AvailablePrintScales_PopulatedByConstructor()
        {
            Assert.That(vm.AvailablePrintScales.Count, Is.GreaterThan(0));
            Assert.That(vm.AvailablePrintScales, Does.Contain("10000"));
        }

        /// <summary>
        /// First control ordinal should default to 1.
        /// </summary>
        [Test]
        public void FirstControlOrdinal_DefaultsTo1()
        {
            Assert.That(vm.FirstControlOrdinal, Is.EqualTo(1));
        }

        /// <summary>
        /// Description kind should default to Symbols.
        /// </summary>
        [Test]
        public void DescKind_DefaultsToSymbols()
        {
            Assert.That(vm.DescKind, Is.EqualTo(DescriptionKind.Symbols));
        }

        /// <summary>
        /// Control label kind should default to Sequence.
        /// </summary>
        [Test]
        public void ControlLabelKind_DefaultsToSequence()
        {
            Assert.That(vm.ControlLabelKind, Is.EqualTo(ControlLabelKind.Sequence));
        }

        // ===== Computed properties =====

        /// <summary>
        /// IsNormalCourse should be true when kind is Normal.
        /// </summary>
        [Test]
        public void IsNormalCourse_TrueWhenNormal()
        {
            vm.CourseKind = CourseKind.Normal;
            Assert.That(vm.IsNormalCourse, Is.True);
            Assert.That(vm.IsScoreCourse, Is.False);
        }

        /// <summary>
        /// IsScoreCourse should be true when kind is Score.
        /// </summary>
        [Test]
        public void IsScoreCourse_TrueWhenScore()
        {
            vm.CourseKind = CourseKind.Score;
            Assert.That(vm.IsScoreCourse, Is.True);
            Assert.That(vm.IsNormalCourse, Is.False);
        }

        /// <summary>
        /// NormalCourseOpacity should be 1.0 for Normal, 0.0 for Score.
        /// </summary>
        [Test]
        public void NormalCourseOpacity_ReflectsCourseKind()
        {
            vm.CourseKind = CourseKind.Normal;
            Assert.That(vm.NormalCourseOpacity, Is.EqualTo(1.0));

            vm.CourseKind = CourseKind.Score;
            Assert.That(vm.NormalCourseOpacity, Is.EqualTo(0.0));
        }

        /// <summary>
        /// ScoreCourseOpacity should be 1.0 for Score, 0.0 for Normal.
        /// </summary>
        [Test]
        public void ScoreCourseOpacity_ReflectsCourseKind()
        {
            vm.CourseKind = CourseKind.Score;
            Assert.That(vm.ScoreCourseOpacity, Is.EqualTo(1.0));

            vm.CourseKind = CourseKind.Normal;
            Assert.That(vm.ScoreCourseOpacity, Is.EqualTo(0.0));
        }

        /// <summary>
        /// IsOkEnabled should be false when course name is empty.
        /// </summary>
        [Test]
        public void IsOkEnabled_FalseWhenNameEmpty()
        {
            Assert.That(vm.IsOkEnabled, Is.False);
        }

        /// <summary>
        /// IsOkEnabled should be true when course name is set and no errors.
        /// </summary>
        [Test]
        public void IsOkEnabled_TrueWhenNameSet()
        {
            vm.CourseName = "Test Course";
            Assert.That(vm.IsOkEnabled, Is.True);
        }

        /// <summary>
        /// IsOkEnabled should be false when there are validation errors.
        /// </summary>
        [Test]
        public void IsOkEnabled_FalseWhenValidationErrors()
        {
            vm.CourseName = "Test Course";
            vm.ClimbText = "not a number";
            Assert.That(vm.IsOkEnabled, Is.False);
        }

        // ===== PropertyChanged notifications =====

        /// <summary>
        /// Setting CourseName should raise PropertyChanged for IsOkEnabled.
        /// </summary>
        [Test]
        public void CourseName_NotifiesIsOkEnabled()
        {
            List<string?> changedProperties = new List<string?>();
            vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            vm.CourseName = "Test";

            Assert.That(changedProperties, Does.Contain("IsOkEnabled"));
        }

        /// <summary>
        /// Setting CourseKind should raise PropertyChanged for all dependent computed properties.
        /// </summary>
        [Test]
        public void CourseKind_NotifiesComputedProperties()
        {
            List<string?> changedProperties = new List<string?>();
            vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

            vm.CourseKind = CourseKind.Score;

            Assert.That(changedProperties, Does.Contain("IsScoreCourse"));
            Assert.That(changedProperties, Does.Contain("IsNormalCourse"));
            Assert.That(changedProperties, Does.Contain("NormalCourseOpacity"));
            Assert.That(changedProperties, Does.Contain("ScoreCourseOpacity"));
            Assert.That(changedProperties, Does.Contain("IsOkEnabled"));
        }

        // ===== CourseKind switching: label kind clamping =====

        /// <summary>
        /// Switching from Score to Normal should clamp score-only label kinds to Sequence.
        /// </summary>
        [Test]
        public void CourseKindToNormal_ClampsScoreOnlyLabelKinds()
        {
            vm.CourseKind = CourseKind.Score;
            vm.ControlLabelKind = ControlLabelKind.SequenceAndScore;

            vm.CourseKind = CourseKind.Normal;

            Assert.That(vm.ControlLabelKind, Is.EqualTo(ControlLabelKind.Sequence));
        }

        /// <summary>
        /// Switching from Score to Normal should preserve valid label kinds.
        /// </summary>
        [Test]
        public void CourseKindToNormal_PreservesValidLabelKinds()
        {
            vm.CourseKind = CourseKind.Score;
            vm.ControlLabelKind = ControlLabelKind.Code;

            vm.CourseKind = CourseKind.Normal;

            Assert.That(vm.ControlLabelKind, Is.EqualTo(ControlLabelKind.Code));
        }

        /// <summary>
        /// Each score-only label kind should be clamped when switching to Normal.
        /// </summary>
        [TestCase(ControlLabelKind.SequenceAndScore)]
        [TestCase(ControlLabelKind.CodeAndScore)]
        [TestCase(ControlLabelKind.Score)]
        public void CourseKindToNormal_ClampsAllScoreLabelKinds(ControlLabelKind scoreKind)
        {
            vm.CourseKind = CourseKind.Score;
            vm.ControlLabelKind = scoreKind;

            vm.CourseKind = CourseKind.Normal;

            Assert.That(vm.ControlLabelKind, Is.EqualTo(ControlLabelKind.Sequence));
        }

        // ===== Climb validation =====

        /// <summary>
        /// Empty climb text should be valid.
        /// </summary>
        [Test]
        public void ClimbValidation_EmptyIsValid()
        {
            vm.ClimbText = "";
            Assert.That(vm.HasErrors, Is.False);
        }

        /// <summary>
        /// Valid numeric climb should be accepted.
        /// </summary>
        [Test]
        public void ClimbValidation_ValidNumberIsAccepted()
        {
            vm.ClimbText = "150";
            Assert.That(vm.HasErrors, Is.False);
        }

        /// <summary>
        /// Non-numeric climb should produce a validation error.
        /// </summary>
        [Test]
        public void ClimbValidation_NonNumericIsInvalid()
        {
            vm.ClimbText = "abc";
            Assert.That(vm.HasErrors, Is.True);
            Assert.That(vm.GetErrors(nameof(vm.ClimbText)).Cast<object>().Any(), Is.True);
        }

        /// <summary>
        /// Negative climb should produce a validation error.
        /// </summary>
        [Test]
        public void ClimbValidation_NegativeIsInvalid()
        {
            vm.ClimbText = "-5";
            Assert.That(vm.HasErrors, Is.True);
        }

        /// <summary>
        /// Climb above 9999 should produce a validation error.
        /// </summary>
        [Test]
        public void ClimbValidation_Above9999IsInvalid()
        {
            vm.ClimbText = "10000";
            Assert.That(vm.HasErrors, Is.True);
        }

        /// <summary>
        /// Climb at boundary values should be valid.
        /// </summary>
        [TestCase("0")]
        [TestCase("9999")]
        [TestCase("500.5")]
        public void ClimbValidation_BoundaryValuesAreValid(string value)
        {
            vm.ClimbText = value;
            Assert.That(vm.HasErrors, Is.False);
        }

        // ===== Length validation =====

        /// <summary>
        /// Empty length text should be valid.
        /// </summary>
        [Test]
        public void LengthValidation_EmptyIsValid()
        {
            vm.LengthText = "";
            Assert.That(vm.HasErrors, Is.False);
        }

        /// <summary>
        /// "Automatic" length text should be valid.
        /// </summary>
        [Test]
        public void LengthValidation_AutomaticIsValid()
        {
            vm.LengthText = MiscText.AutomaticLength;
            Assert.That(vm.HasErrors, Is.False);
        }

        /// <summary>
        /// Valid numeric length should be accepted.
        /// </summary>
        [Test]
        public void LengthValidation_ValidNumberIsAccepted()
        {
            vm.LengthText = "5.2";
            Assert.That(vm.HasErrors, Is.False);
        }

        /// <summary>
        /// Non-numeric length should produce a validation error.
        /// </summary>
        [Test]
        public void LengthValidation_NonNumericIsInvalid()
        {
            vm.LengthText = "xyz";
            Assert.That(vm.HasErrors, Is.True);
        }

        /// <summary>
        /// Zero length should produce a validation error.
        /// </summary>
        [Test]
        public void LengthValidation_ZeroIsInvalid()
        {
            vm.LengthText = "0";
            Assert.That(vm.HasErrors, Is.True);
        }

        /// <summary>
        /// Length of 100 or more should produce a validation error.
        /// </summary>
        [Test]
        public void LengthValidation_100OrMoreIsInvalid()
        {
            vm.LengthText = "100";
            Assert.That(vm.HasErrors, Is.True);
        }

        // ===== CourseName validation =====

        /// <summary>
        /// No validation error on initial empty name (validation hasn't run).
        /// </summary>
        [Test]
        public void CourseNameValidation_NoErrorOnInitialEmpty()
        {
            Assert.That(vm.HasErrors, Is.False);
        }

        /// <summary>
        /// Setting name then clearing it should produce a Required validation error.
        /// </summary>
        [Test]
        public void CourseNameValidation_ClearingNameProducesError()
        {
            vm.CourseName = "Test";
            vm.CourseName = "";
            Assert.That(vm.HasErrors, Is.True);
            Assert.That(vm.GetErrors(nameof(vm.CourseName)).Cast<object>().Any(), Is.True);
        }

        /// <summary>
        /// Setting a valid name should clear validation errors.
        /// </summary>
        [Test]
        public void CourseNameValidation_SettingNameClearsError()
        {
            vm.CourseName = "Test";
            vm.CourseName = "";
            Assert.That(vm.HasErrors, Is.True);

            vm.CourseName = "Valid Name";
            Assert.That(vm.HasErrors, Is.False);
        }

        // ===== Validation clears when errors are fixed =====

        /// <summary>
        /// Fixing an invalid climb should clear the error.
        /// </summary>
        [Test]
        public void ClimbValidation_FixingValueClearsError()
        {
            vm.ClimbText = "bad";
            Assert.That(vm.HasErrors, Is.True);

            vm.ClimbText = "100";
            Assert.That(vm.HasErrors, Is.False);
        }

        /// <summary>
        /// Fixing an invalid length should clear the error.
        /// </summary>
        [Test]
        public void LengthValidation_FixingValueClearsError()
        {
            vm.LengthText = "bad";
            Assert.That(vm.HasErrors, Is.True);

            vm.LengthText = "5.0";
            Assert.That(vm.HasErrors, Is.False);
        }

        // ===== IsOkEnabled interaction with validation =====

        /// <summary>
        /// IsOkEnabled should become false when a validation error is introduced.
        /// </summary>
        [Test]
        public void IsOkEnabled_BecomesFalseOnValidationError()
        {
            vm.CourseName = "Test";
            Assert.That(vm.IsOkEnabled, Is.True);

            vm.ClimbText = "invalid";
            Assert.That(vm.IsOkEnabled, Is.False);
        }

        /// <summary>
        /// IsOkEnabled should become true when validation error is fixed.
        /// </summary>
        [Test]
        public void IsOkEnabled_BecomesTrueWhenErrorFixed()
        {
            vm.CourseName = "Test";
            vm.ClimbText = "invalid";
            Assert.That(vm.IsOkEnabled, Is.False);

            vm.ClimbText = "";
            Assert.That(vm.IsOkEnabled, Is.True);
        }

        // ===== ScoreColumn convenience property =====

        /// <summary>
        /// ScoreColumn getter should map index values to model values.
        /// </summary>
        [TestCase(0, 0)]   // Column A
        [TestCase(1, 1)]   // Column B
        [TestCase(2, 7)]   // Column H
        [TestCase(3, -1)]  // None
        public void ScoreColumn_GetMapsIndexToModelValue(int index, int expected)
        {
            vm.CourseKind = CourseKind.Score;
            vm.ScoreColumnIndex = index;
            Assert.That(vm.ScoreColumn, Is.EqualTo(expected));
        }

        /// <summary>
        /// ScoreColumn should return -1 for normal courses regardless of index.
        /// </summary>
        [Test]
        public void ScoreColumn_ReturnsNegativeOneForNormalCourse()
        {
            vm.CourseKind = CourseKind.Normal;
            vm.ScoreColumnIndex = 0;
            Assert.That(vm.ScoreColumn, Is.EqualTo(-1));
        }

        /// <summary>
        /// ScoreColumn setter should map model values back to index values.
        /// </summary>
        [TestCase(0, 0)]   // Column A
        [TestCase(1, 1)]   // Column B
        [TestCase(7, 2)]   // Column H
        [TestCase(-1, 3)]  // None
        [TestCase(5, 3)]   // Unknown → None
        public void ScoreColumn_SetMapsModelValueToIndex(int modelValue, int expectedIndex)
        {
            vm.ScoreColumn = modelValue;
            Assert.That(vm.ScoreColumnIndex, Is.EqualTo(expectedIndex));
        }

        // ===== PrintScale convenience property =====

        /// <summary>
        /// PrintScale getter should parse the text as a float.
        /// </summary>
        [Test]
        public void PrintScale_GetParsesText()
        {
            vm.PrintScaleText = "15000";
            Assert.That(vm.PrintScale, Is.EqualTo(15000f));
        }

        /// <summary>
        /// PrintScale getter should return 0 for unparseable text.
        /// </summary>
        [Test]
        public void PrintScale_GetReturnsZeroForInvalidText()
        {
            vm.PrintScaleText = "abc";
            Assert.That(vm.PrintScale, Is.EqualTo(0f));
        }

        /// <summary>
        /// PrintScale setter should update the text.
        /// </summary>
        [Test]
        public void PrintScale_SetUpdatesText()
        {
            vm.PrintScale = 7500;
            Assert.That(vm.PrintScaleText, Is.EqualTo("7500"));
        }

        // ===== Climb convenience property =====

        /// <summary>
        /// Climb getter should return -1 for empty text.
        /// </summary>
        [Test]
        public void Climb_GetReturnsNegativeOneForEmpty()
        {
            vm.ClimbText = "";
            Assert.That(vm.Climb, Is.EqualTo(-1f));
        }

        /// <summary>
        /// Climb getter should parse the text as a float.
        /// </summary>
        [Test]
        public void Climb_GetParsesText()
        {
            vm.ClimbText = "250";
            Assert.That(vm.Climb, Is.EqualTo(250f));
        }

        /// <summary>
        /// Climb setter should clear text for negative values.
        /// </summary>
        [Test]
        public void Climb_SetClearsTextForNegative()
        {
            vm.Climb = -1;
            Assert.That(vm.ClimbText, Is.EqualTo(""));
        }

        /// <summary>
        /// Climb setter should set text for positive values.
        /// </summary>
        [Test]
        public void Climb_SetUpdatesText()
        {
            vm.Climb = 300;
            Assert.That(vm.ClimbText, Is.EqualTo("300"));
        }

        // ===== Length convenience property =====

        /// <summary>
        /// Length getter should return null for empty text.
        /// </summary>
        [Test]
        public void Length_GetReturnsNullForEmpty()
        {
            vm.LengthText = "";
            Assert.That(vm.Length, Is.Null);
        }

        /// <summary>
        /// Length getter should return null for "Automatic" text.
        /// </summary>
        [Test]
        public void Length_GetReturnsNullForAutomatic()
        {
            vm.LengthText = MiscText.AutomaticLength;
            Assert.That(vm.Length, Is.Null);
        }

        /// <summary>
        /// Length getter should convert km text to meters.
        /// </summary>
        [Test]
        public void Length_GetConvertsKmToMeters()
        {
            vm.LengthText = "5.2";
            Assert.That(vm.Length, Is.EqualTo(5200f));
        }

        /// <summary>
        /// Length setter should convert meters to km text.
        /// </summary>
        [Test]
        public void Length_SetConvertsMetersToKm()
        {
            vm.Length = 3500;
            Assert.That(vm.LengthText, Is.EqualTo("3.5"));
        }

        /// <summary>
        /// Length setter should set "Automatic" text for null.
        /// </summary>
        [Test]
        public void Length_SetUsesAutomaticForNull()
        {
            vm.Length = null;
            Assert.That(vm.LengthText, Is.EqualTo(MiscText.AutomaticLength));
        }

        // ===== SecondaryTitlePipeDelimited =====

        /// <summary>
        /// Pipe-delimited getter should return null for empty secondary title.
        /// </summary>
        [Test]
        public void SecondaryTitlePipeDelimited_GetReturnsNullForEmpty()
        {
            vm.SecondaryTitle = "";
            Assert.That(vm.SecondaryTitlePipeDelimited, Is.Null);
        }

        /// <summary>
        /// Pipe-delimited getter should convert newlines to pipes.
        /// </summary>
        [Test]
        public void SecondaryTitlePipeDelimited_GetConvertsNewlinesToPipes()
        {
            vm.SecondaryTitle = "Line 1\r\nLine 2\r\nLine 3";
            Assert.That(vm.SecondaryTitlePipeDelimited, Is.EqualTo("Line 1|Line 2|Line 3"));
        }

        /// <summary>
        /// Pipe-delimited setter should convert pipes to newlines.
        /// </summary>
        [Test]
        public void SecondaryTitlePipeDelimited_SetConvertsPipesToNewlines()
        {
            vm.SecondaryTitlePipeDelimited = "Line 1|Line 2";
            Assert.That(vm.SecondaryTitle, Is.EqualTo("Line 1\r\nLine 2"));
        }

        /// <summary>
        /// Pipe-delimited setter should handle null by clearing secondary title.
        /// </summary>
        [Test]
        public void SecondaryTitlePipeDelimited_SetNullClearsTitle()
        {
            vm.SecondaryTitlePipeDelimited = null;
            Assert.That(vm.SecondaryTitle, Is.EqualTo(""));
        }
    }
}
