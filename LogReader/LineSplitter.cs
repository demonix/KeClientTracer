using System;
using System.Globalization;

namespace LogReader
{/*
    public class LineSplitter
    {
        private string[] _fields;
        private int _nextFieldIndex = 0;
        private int _nextFieldStart = 0;

        private string ReadField(int field, bool initializing, bool discardValue)
        {
            if (!initializing)
            {
               
               
               
                if (_fields[field] != null)
                    return _fields[field];
            }


            int index = _nextFieldIndex;

            while (index < field + 1)
            {
                // Handle case where stated start of field is past buffer
                // This can occur because _nextFieldStart is simply 1 + last char position of previous field
                if (_nextFieldStart == _bufferLength)
                {
                    _nextFieldStart = 0;

                    // Possible EOF will be handled later (see Handle_EOF1)
                    ReadBuffer();
                }

                string value = null;

                if (_missingFieldFlag)
                {
                    value = HandleMissingField(value, index, ref _nextFieldStart);
                }
                else if (_nextFieldStart == _bufferLength)
                {
                    // Handle_EOF1: Handle EOF here

                    // If current field is the requested field, then the value of the field is "" as in "f1,f2,f3,(\s*)"
                    // otherwise, the CSV is malformed

                    if (index == field)
                    {
                        if (!discardValue)
                        {
                            value = string.Empty;
                            _fields[index] = value;
                        }
                    }
                    else
                    {
                        value = HandleMissingField(value, index, ref _nextFieldStart);
                    }
                }
                else
                {
                    // Trim spaces at start
                    if ((_trimmingOptions & ValueTrimmingOptions.UnquotedOnly) != 0)
                        SkipWhiteSpaces(ref _nextFieldStart);

                    if (_eof)
                    {
                        value = string.Empty;
                        _fields[field] = value;
                    }
                    else if (_buffer[_nextFieldStart] != _quote)
                    {
                        // Non-quoted field

                        int start = _nextFieldStart;
                        int pos = _nextFieldStart;

                        for (; ; )
                        {
                            while (pos < _bufferLength)
                            {
                                char c = _buffer[pos];

                                if (c == _delimiter)
                                {
                                    _nextFieldStart = pos + 1;

                                    break;
                                }
                                else if (c == '\r' || c == '\n')
                                {
                                    _nextFieldStart = pos;
                                    _eol = true;

                                    break;
                                }
                                else
                                    pos++;
                            }

                            if (pos < _bufferLength)
                                break;
                            else
                            {
                                if (!discardValue)
                                    value += new string(_buffer, start, pos - start);

                                start = 0;
                                pos = 0;
                                _nextFieldStart = 0;

                                if (!ReadBuffer())
                                    break;
                            }
                        }

                        if (!discardValue)
                        {
                            if ((_trimmingOptions & ValueTrimmingOptions.UnquotedOnly) == 0)
                            {
                                if (!_eof && pos > start)
                                    value += new string(_buffer, start, pos - start);
                            }
                            else
                            {
                                if (!_eof && pos > start)
                                {
                                    // Do the trimming
                                    pos--;
                                    while (pos > -1 && IsWhiteSpace(_buffer[pos]))
                                        pos--;
                                    pos++;

                                    if (pos > 0)
                                        value += new string(_buffer, start, pos - start);
                                }
                                else
                                    pos = -1;

                                // If pos <= 0, that means the trimming went past buffer start,
                                // and the concatenated value needs to be trimmed too.
                                if (pos <= 0)
                                {
                                    pos = (value == null ? -1 : value.Length - 1);

                                    // Do the trimming
                                    while (pos > -1 && IsWhiteSpace(value[pos]))
                                        pos--;

                                    pos++;

                                    if (pos > 0 && pos != value.Length)
                                        value = value.Substring(0, pos);
                                }
                            }

                            if (value == null)
                                value = string.Empty;
                        }

                        if (_eol || _eof)
                        {
                            _eol = ParseNewLine(ref _nextFieldStart);

                            // Reaching a new line is ok as long as the parser is initializing or it is the last field
                            if (!initializing && index != _fieldCount - 1)
                            {
                                if (value != null && value.Length == 0)
                                    value = null;

                                value = HandleMissingField(value, index, ref _nextFieldStart);
                            }
                        }

                        if (!discardValue)
                            _fields[index] = value;
                    }
                    else
                    {
                        // Quoted field

                        // Skip quote
                        int start = _nextFieldStart + 1;
                        int pos = start;

                        bool quoted = true;
                        bool escaped = false;

                        if ((_trimmingOptions & ValueTrimmingOptions.QuotedOnly) != 0)
                        {
                            SkipWhiteSpaces(ref start);
                            pos = start;
                        }

                        for (; ; )
                        {
                            while (pos < _bufferLength)
                            {
                                char c = _buffer[pos];

                                if (escaped)
                                {
                                    escaped = false;
                                    start = pos;
                                }
                                // IF current char is escape AND (escape and quote are different OR next char is a quote)
                                else if (c == _escape && (_escape != _quote || (pos + 1 < _bufferLength && _buffer[pos + 1] == _quote) || (pos + 1 == _bufferLength && _reader.Peek() == _quote)))
                                {
                                    if (!discardValue)
                                        value += new string(_buffer, start, pos - start);

                                    escaped = true;
                                }
                                else if (c == _quote)
                                {
                                    quoted = false;
                                    break;
                                }

                                pos++;
                            }

                            if (!quoted)
                                break;
                            else
                            {
                                if (!discardValue && !escaped)
                                    value += new string(_buffer, start, pos - start);

                                start = 0;
                                pos = 0;
                                _nextFieldStart = 0;

                                if (!ReadBuffer())
                                {
                                    HandleParseError(new MalformedCsvException(GetCurrentRawData(), _nextFieldStart, Math.Max(0, _currentRecordIndex), index), ref _nextFieldStart);
                                    return null;
                                }
                            }
                        }

                        if (!_eof)
                        {
                            // Append remaining parsed buffer content
                            if (!discardValue && pos > start)
                                value += new string(_buffer, start, pos - start);

                            if (!discardValue && value != null && (_trimmingOptions & ValueTrimmingOptions.QuotedOnly) != 0)
                            {
                                int newLength = value.Length;
                                while (newLength > 0 && IsWhiteSpace(value[newLength - 1]))
                                    newLength--;

                                if (newLength < value.Length)
                                    value = value.Substring(0, newLength);
                            }

                            // Skip quote
                            _nextFieldStart = pos + 1;

                            // Skip whitespaces between the quote and the delimiter/eol
                            SkipWhiteSpaces(ref _nextFieldStart);

                            // Skip delimiter
                            bool delimiterSkipped;
                            if (_nextFieldStart < _bufferLength && _buffer[_nextFieldStart] == _delimiter)
                            {
                                _nextFieldStart++;
                                delimiterSkipped = true;
                            }
                            else
                            {
                                delimiterSkipped = false;
                            }

                            // Skip new line delimiter if initializing or last field
                            // (if the next field is missing, it will be caught when parsed)
                            if (!_eof && !delimiterSkipped && (initializing || index == _fieldCount - 1))
                                _eol = ParseNewLine(ref _nextFieldStart);

                            // If no delimiter is present after the quoted field and it is not the last field, then it is a parsing error
                            if (!delimiterSkipped && !_eof && !(_eol || IsNewLine(_nextFieldStart)))
                                HandleParseError(new MalformedCsvException(GetCurrentRawData(), _nextFieldStart, Math.Max(0, _currentRecordIndex), index), ref _nextFieldStart);
                        }

                        if (!discardValue)
                        {
                            if (value == null)
                                value = string.Empty;

                            _fields[index] = value;
                        }
                    }
                }

                _nextFieldIndex = Math.Max(index + 1, _nextFieldIndex);

                if (index == field)
                {
                    // If initializing, return null to signify the last field has been reached

                    if (initializing)
                    {
                        if (_eol || _eof)
                            return null;
                        else
                            return string.IsNullOrEmpty(value) ? string.Empty : value;
                    }
                    else
                        return value;
                }

                index++;
            }

            // Getting here is bad ...
            HandleParseError(new MalformedCsvException(GetCurrentRawData(), _nextFieldStart, Math.Max(0, _currentRecordIndex), index), ref _nextFieldStart);
            return null;
        }

    }
  * */
}