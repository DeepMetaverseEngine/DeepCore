// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "./OwnableEvent.sol";

interface OwnableInterface is OwnableEvent {

    function owner() external view returns (address);

    function renounceOwnership() external;

    function transferOwnership(address newOwner) external;

    function setAssociatedOperator(address newAssociatedOperator, bool flag) external;

}
