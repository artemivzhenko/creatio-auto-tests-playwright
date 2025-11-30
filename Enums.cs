namespace CreatioAutoTestsPlaywright.Frontend
{
    /// <summary>
    /// Field type enumeration used for dynamic page building and validations.
    /// Each value corresponds to a specific field class implementation.
    /// </summary>
    public enum FieldType
    {
        /// <summary>
        /// Simple text field implemented by TextField class.
        /// </summary>
        TextField = 1,
        DateTimeField = 2,
        NumberField = 3,
        BooleanField = 4,
    }

    /// <summary>
    /// Logical content type of a text field.
    /// Used to choose parsing/validation strategy.
    /// </summary>
    public enum TextFieldTypeEnum
    {
        Text,
        RichText,
        Email,
        PhoneNumber,
        Link
    }

    /// <summary>
    /// Logical type of Freedom UI Date/Time field in Creatio.
    /// - Time:   time only, e.g. "7:36 AM"
    /// - Date:   date only, e.g. "03/27/2025"
    /// - DateTime: date and time, e.g. "03/27/2025 7:36 AM"
    /// </summary>
    public enum DateTimeFieldTypeEnum
    {
        Time = 1,
        Date = 2,
        DateTime = 3
    }

    /// <summary>
    /// Logical numeric type of Freedom UI number field.
    /// - Integer: whole numbers only.
    /// - Decimal: fractional numbers with decimal separator.
    /// </summary>
    public enum NumberFieldTypeEnum
    {
        Integer = 1,
        Decimal = 2
    }
}
