// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/ownable/OwnableInterface.sol";
import "../../nameserver/nameServiceRef/NameServiceRefInterface.sol";
import "./DeputyCenterEvent.sol";

import "./DeputyCenterType.sol";

interface DeputyCenterInterface is OwnableInterface, NameServiceRefInterface, DeputyCenterEvent {

    function dispatchTransactions(DeputyCenterType.BatchTransactions[] memory bTxs) external;

    function calledAndCaller() view external returns (bool called, address caller);
}
