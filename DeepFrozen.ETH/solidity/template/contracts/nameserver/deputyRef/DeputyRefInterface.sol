// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/nameServiceRef/NameServiceRefInterface.sol";
import "./DeputyRefEvent.sol";

interface DeputyRefInterface is NameServiceRefInterface, DeputyRefEvent {

}
