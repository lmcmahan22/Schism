// <copyright file="NamingConstants.cs" company="Precision Valve &amp; Automation (PVA)">
// Copyright (c) Precision Valve &amp; Automation (PVA). All rights reserved.
// </copyright>

namespace Schiism.Core
{
    /// <summary>
    /// Provides constant values for named pipes used in inter-process communication.
    /// </summary>
    public static class NamingConstants
    {
        /// <summary>
        /// Represents the name of the Modbus data stream.
        /// </summary>
        public const string ModbusDataStreamName = "schiism.modbusData.stream.v1";

        /// <summary>
        /// Represents the name of the Connection Diagnostics data stream.
        /// </summary>
        public const string ConnDiagStreamName = "schiism.connDiag.stream.v1";

        /// <summary>
        /// Represents the name of the Settings Configuration command (Console --> Service).
        /// </summary>
        public const string SettingsCommandName = "schiism.settings.cmd.v1";

        /// <summary>
        /// Represents the name of the Modbus Write command (Console --> Service).
        /// </summary>
        public const string ModbusWriteCommandName = "schiism.modbusWrite.cmd.v1";

        /// <summary>
        /// Represents the name of the Initializing Settings Configuration command (Service --> Console).
        /// </summary>
        public const string InitSettingsCommandName = "schiism.initSettings.cmd.v1";

        /// <summary>
        /// Represents the name of the Board Available command (Service --> Console).
        /// </summary>
        public const string BoardAvailableCommandName = "schiism.boardAvailable.cmd.v1";

        public const string ServiceName = "PVAModbusClient";
    }
}
