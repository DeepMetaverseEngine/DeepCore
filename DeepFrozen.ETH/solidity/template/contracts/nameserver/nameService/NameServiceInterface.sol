// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/accessControl/AccessControlInterface.sol";
import "./NameServiceEvent.sol";

import "./NameServiceType.sol";

interface NameServiceInterface is AccessControlInterface, NameServiceEvent {

    function isMultiple(bytes32 keyName, address which) view external returns (bool);

    function isMultipleSafe(bytes32 keyName, address which) view external returns (bool);

    function getSingle(bytes32 keyName) view external returns (address);

    function getSingleSafe(bytes32 keyName) view external returns (address);

    //==========

    function setMultiple(bytes32 keyName, address which, bool enable) external;

    function setMultipleEntries(bytes32[] memory keyName, address[] memory which, bool[] memory enable) external;

    function setSingle(bytes32 keyName, address which, bool enable) external;

    function setSingleEntries(bytes32[] memory keyName, address[] memory which, bool[] memory enable) external;

    function listSingleEntries() view external returns (NameServiceType.SingleEntryRet[] memory);

    function listMultipleEntries() view external returns (NameServiceType.MultipleEntryRet[] memory);
}
