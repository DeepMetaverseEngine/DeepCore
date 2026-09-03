// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../erc165/HERC165Interface.sol";
import "../../nameserver/context/ContextInterface.sol";
import "./HERC1155Event.sol";

interface HERC1155Interface is HERC165Interface, ContextInterface, IERC1155, IERC1155MetadataURI, HERC1155Event {

}
