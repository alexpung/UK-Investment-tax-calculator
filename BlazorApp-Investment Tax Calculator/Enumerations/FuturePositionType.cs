﻿using System.ComponentModel;

namespace Enumerations;

public enum FuturePositionType
{
    [Description("Open long position")]
    OPENLONG,
    [Description("Open short position")]
    OPENSHORT,
    [Description("Close long position")]
    CLOSELONG,
    [Description("Close short position")]
    CLOSESHORT
}
