using System;
using System.Collections.Generic;
using Solhigson.Framework.Utilities;

namespace Solhigson.Framework.Dto;

public class PagedSearchParameters
{
    private bool _convertedToDateToUniversalTime;
    private bool _convertedFromDateToUniversalTime;
        
    public string DateColumnName { get; set; }
    public PagedSearchParameters()
    {
        OtherParameters = new Dictionary<string, string>();
        PageSize = 20;
        Page = 1;
        _convertedFromDateToUniversalTime = false;
        _convertedToDateToUniversalTime = false;
    }
        
    public void MarkDatesAsUniversalTime()
    {
        _convertedFromDateToUniversalTime = true;
        _convertedToDateToUniversalTime = true;
    }

    public void SetToDateToEndOfDay()
    {
        _toDate = _toDate?.Date.AddDays(1).AddMilliseconds(-1);
    }

    public void ConvertToUniversalTime(int? offset = null)
    {
        offset ??= LocaleUtil.GetTimeZoneOffset() * -1;
        if (_convertedFromDateToUniversalTime)
        {
            return;
        }
        _fromDate = _fromDate?.AddMinutes(offset.Value);
        _toDate = _toDate == null 
            ? ToDate 
            : _toDate?.AddMinutes(offset.Value);
        _convertedFromDateToUniversalTime = true;
    }
        
    public int PageSize { get; set; }

    public int Page { get; set; }

    public long TotalRecords { get; set; }
        
    public string OrderBy { get; set; }

    private DateTime? _fromDate;
    public DateTime FromDate 
    {
        get
        {
            if (_fromDate.HasValue)
            {
                return _fromDate.Value;
            }
            _fromDate = DateTime.UtcNow.Date.AddMinutes(LocaleUtil.GetTimeZoneOffset() * -1);
            _convertedFromDateToUniversalTime = true;
            return _fromDate.Value;
        }
        set => _fromDate = value;
    }

    private DateTime? _toDate;
    public DateTime ToDate
    {
        get
        {
            if (_toDate.HasValue)
            {
                return _toDate.Value;
            }
            _toDate = FromDate.AddDays(1);
            _convertedToDateToUniversalTime = true;
            return _toDate.Value;
        }
        set => _toDate = value;
    }


    public Dictionary<string, string> OtherParameters { get; set; }

    public string this[string name] => OtherParameters.TryGetValue(name, out var value) ? value : null;

    public Dictionary<string, string>.KeyCollection Keys => this.OtherParameters.Keys;
        
    public void Add(string name, object value)
    {
        var result = Convert.ToString(value);
        if (!string.IsNullOrWhiteSpace(result))
        {
            OtherParameters[name] = result;
        }
    }

}