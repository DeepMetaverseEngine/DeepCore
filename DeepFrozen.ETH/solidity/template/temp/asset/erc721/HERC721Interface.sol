// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../preclude/Preclude.sol";

import "../erc165/HERC165Interface.sol";
import "../context/ContextInterface.sol";
import "./HERC721Event.sol";

interface HERC721Interface is HERC165Interface, ContextInterface, IERC721, IERC721Metadata, IERC721Enumerable, HERC721Event {

}
