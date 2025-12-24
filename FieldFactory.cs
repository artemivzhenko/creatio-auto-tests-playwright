using System;
using CreatioAutoTestsPlaywright.Config;

namespace CreatioAutoTestsPlaywright.Frontend
{
    /// <summary>
    /// Factory responsible for creating concrete field instances from JSON configuration.
    /// It converts JSON "type" / "subtype" strings into enum values and passes them to field classes.
    /// </summary>
    public static class FieldFactory
    {
        /// <summary>
        /// Creates a concrete field instance based on the given configuration.
        /// </summary>
        /// <param name="page">Creatio page object used by the field.</param>
        /// <param name="cfg">Field configuration loaded from JSON.</param>
        /// <returns>Concrete implementation of IField.</returns>
        public static IField CreateField(CreatioPage page, FieldConfig cfg)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            if (cfg == null)
            {
                throw new ArgumentNullException(nameof(cfg));
            }

            if (string.IsNullOrWhiteSpace(cfg.Code))
            {
                throw new ArgumentException("FieldConfig.Code must not be empty.", nameof(cfg));
            }

            var fieldType = ParseFieldType(cfg.Type);

            switch (fieldType)
            {
                case FieldType.TextField:
                    {
                        var textSubtype = ParseTextFieldSubtype(cfg.Subtype);

                        return new TextField(
                            page: page,
                            title: cfg.Title,
                            code: cfg.Code,
                            required: cfg.Required,
                            readOnly: cfg.ReadOnly,
                            placeholder: cfg.Placeholder,
                            contentType: textSubtype);
                    }

                case FieldType.NumberField:
                    {
                        var numSubtype = ParseNumberFieldSubtype(cfg.Subtype);

                        return new NumberField(
                            page: page,
                            title: cfg.Title,
                            code: cfg.Code,
                            required: cfg.Required,
                            readOnly: cfg.ReadOnly,
                            placeholder: cfg.Placeholder,
                            numberType: numSubtype);
                    }

                case FieldType.DateTimeField:
                    {
                        var dtSubtype = ParseDateTimeFieldSubtype(cfg.Subtype);

                        return new DateTimeField(
                            page: page,
                            title: cfg.Title,
                            code: cfg.Code,
                            required: cfg.Required,
                            readOnly: cfg.ReadOnly,
                            placeholder: cfg.Placeholder,
                            dateTimeType: dtSubtype);
                    }

                case FieldType.BooleanField:
                    {
                        // BooleanField does not use required/placeholder in constructor.
                        // Internally Required is always false and placeholder is not used. :contentReference[oaicite:2]{index=2}
                        return new BooleanField(
                            page: page,
                            title: cfg.Title,
                            code: cfg.Code,
                            readOnly: cfg.ReadOnly);
                    }

                case FieldType.LookupField:
                    {
                        // LookupField does not have subtype and directly uses BaseField constructor. :contentReference[oaicite:3]{index=3}
                        return new LookupField(
                            page: page,
                            title: cfg.Title,
                            code: cfg.Code,
                            required: cfg.Required,
                            readOnly: cfg.ReadOnly,
                            placeholder: cfg.Placeholder);
                    }

                default:
                    throw new NotSupportedException(
                        $"Field type '{cfg.Type}' is not supported for field Code='{cfg.Code}'.");
            }
        }

        /// <summary>
        /// Converts JSON "type" string into FieldType enum.
        /// Accepts values like "Text", "Number", "DateTime", "Boolean", "Lookup"
        /// and also enum names like "TextField", "NumberField" if used in JSON.
        /// </summary>
        private static FieldType ParseFieldType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException("Field type (FieldConfig.Type) must not be empty.");
            }

            var value = type.Trim();

            switch (value.ToLowerInvariant())
            {
                case "text":
                case "textfield":
                    return FieldType.TextField;

                case "number":
                case "numberfield":
                    return FieldType.NumberField;

                case "datetime":
                case "datetimefield":
                    return FieldType.DateTimeField;

                case "boolean":
                case "bool":
                case "booleanfield":
                    return FieldType.BooleanField;

                case "lookup":
                case "lookupfield":
                    return FieldType.LookupField;

                default:
                    throw new NotSupportedException(
                        $"Unknown field type string '{type}'. Expected: Text, Number, DateTime, Boolean, Lookup.");
            }
        }

        /// <summary>
        /// Converts JSON "subtype" string into TextFieldTypeEnum.
        /// Default is Text when subtype is empty.
        /// </summary>
        private static TextFieldTypeEnum ParseTextFieldSubtype(string? subtype)
        {
            if (string.IsNullOrWhiteSpace(subtype))
            {
                // Default logical subtype for text fields. :contentReference[oaicite:4]{index=4}
                return TextFieldTypeEnum.Text;
            }

            var value = subtype.Trim().ToLowerInvariant();

            return value switch
            {
                "text" => TextFieldTypeEnum.Text,
                "richtext" => TextFieldTypeEnum.RichText,
                "rich_text" => TextFieldTypeEnum.RichText,
                "email" => TextFieldTypeEnum.Email,
                "phone" => TextFieldTypeEnum.PhoneNumber,
                "phonenumber" => TextFieldTypeEnum.PhoneNumber,
                "phone_number" => TextFieldTypeEnum.PhoneNumber,
                "link" => TextFieldTypeEnum.Link,

                _ => throw new NotSupportedException(
                    $"Unknown text field subtype '{subtype}'. Expected: Text, RichText, Email, PhoneNumber, Link.")
            };
        }

        /// <summary>
        /// Converts JSON "subtype" string into NumberFieldTypeEnum.
        /// Default is Integer when subtype is empty.
        /// </summary>
        private static NumberFieldTypeEnum ParseNumberFieldSubtype(string? subtype)
        {
            if (string.IsNullOrWhiteSpace(subtype))
            {
                return NumberFieldTypeEnum.Integer;
            }

            var value = subtype.Trim().ToLowerInvariant();

            return value switch
            {
                "integer" => NumberFieldTypeEnum.Integer,
                "int" => NumberFieldTypeEnum.Integer,
                "decimal" => NumberFieldTypeEnum.Decimal,

                _ => throw new NotSupportedException(
                    $"Unknown number field subtype '{subtype}'. Expected: Integer, Decimal.")
            };
        }

        /// <summary>
        /// Converts JSON "subtype" string into DateTimeFieldTypeEnum.
        /// Default is DateTime when subtype is empty.
        /// </summary>
        private static DateTimeFieldTypeEnum ParseDateTimeFieldSubtype(string? subtype)
        {
            if (string.IsNullOrWhiteSpace(subtype))
            {
                return DateTimeFieldTypeEnum.DateTime;
            }

            var value = subtype.Trim().ToLowerInvariant();

            return value switch
            {
                "time" => DateTimeFieldTypeEnum.Time,
                "date" => DateTimeFieldTypeEnum.Date,
                "datetime" => DateTimeFieldTypeEnum.DateTime,

                _ => throw new NotSupportedException(
                    $"Unknown DateTime field subtype '{subtype}'. Expected: Time, Date, DateTime.")
            };
        }
    }
}