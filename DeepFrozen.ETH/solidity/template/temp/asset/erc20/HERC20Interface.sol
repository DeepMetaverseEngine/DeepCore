// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../preclude/Preclude.sol";

import "../context/ContextInterface.sol";
import "./HERC20Event.sol";

interface HERC20Interface is ContextInterface, IERC20, IERC20Metadata, HERC20Event {

    function cap() external view returns (uint256);

}
