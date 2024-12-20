﻿using System.Collections.Generic;

namespace Solhigson.Utilities.Pluralization;

internal class BidirectionalDictionary<TFirst, TSecond>
{
    internal Dictionary<TFirst, TSecond> FirstToSecondDictionary { get; set; }

    internal Dictionary<TSecond, TFirst> SecondToFirstDictionary { get; set; }

    internal BidirectionalDictionary()
    {
        this.FirstToSecondDictionary = new Dictionary<TFirst, TSecond>();
        this.SecondToFirstDictionary = new Dictionary<TSecond, TFirst>();
    }

    internal BidirectionalDictionary(Dictionary<TFirst, TSecond> firstToSecondDictionary)
        : this()
    {
        foreach (TFirst firstValue in firstToSecondDictionary.Keys)
            this.AddValue(firstValue, firstToSecondDictionary[firstValue]);
    }

    internal virtual bool ExistsInFirst(TFirst value)
    {
        return this.FirstToSecondDictionary.ContainsKey(value);
    }

    internal virtual bool ExistsInSecond(TSecond value)
    {
        return this.SecondToFirstDictionary.ContainsKey(value);
    }

    internal virtual TSecond GetSecondValue(TFirst value)
    {
        if (this.ExistsInFirst(value))
            return this.FirstToSecondDictionary[value];
        else
            return default(TSecond);
    }

    internal virtual TFirst GetFirstValue(TSecond value)
    {
        if (this.ExistsInSecond(value))
            return this.SecondToFirstDictionary[value];
        else
            return default(TFirst);
    }

    internal void AddValue(TFirst firstValue, TSecond secondValue)
    {
        this.FirstToSecondDictionary.Add(firstValue, secondValue);
        if (this.SecondToFirstDictionary.ContainsKey(secondValue))
            return;
        this.SecondToFirstDictionary.Add(secondValue, firstValue);
    }
}