// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/ownable/OwnableInterface.sol";
import "./AccessControlEvent.sol";

interface AccessControlInterface is OwnableInterface, AccessControlEvent {

    function isBlocked(address which) view external returns (bool);

    function isPrivileged(address which) view external returns (bool);

    //==========

    function setBlockList(address[] memory list, bool flag) external;

    function setPrivilegedList(address[] memory list, bool flag) external;
}
