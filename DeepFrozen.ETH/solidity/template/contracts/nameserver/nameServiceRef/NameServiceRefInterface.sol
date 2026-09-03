// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/accessControlRef/AccessControlRefInterface.sol";
import "./NameServiceRefEvent.sol";

interface NameServiceRefInterface is AccessControlRefInterface, NameServiceRefEvent {

}
