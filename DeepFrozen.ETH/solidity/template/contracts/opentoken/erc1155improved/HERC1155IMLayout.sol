// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/ownable/OwnableLayout.sol";
import "../../nameserver/nameServiceRef/NameServiceRefLayout.sol";
import "../../nameserver/reentrancy/ReentrancyLayout.sol";
import "../erc1155/HERC1155Layout.sol";

abstract contract HERC1155IMLayout is OwnableLayout, NameServiceRefLayout, ReentrancyLayout, HERC1155Layout {

    bool internal _supportTransfer;
    bool internal _supportMint;
    bool internal _sudoMint;
    bool internal _supportBurn;
    bool internal _sudoBurn;

    //statistic
    uint256 internal _transferTxs;
    //storage all types
    uint256 internal _transferAmounts;

    mapping(address => bool) internal _interactAccount;
    uint256 internal _interactAmounts;

    mapping(address => bool) internal _blockListFrom;
    mapping(address => bool) internal _blockListTo;
    mapping(address => bool) internal _privilegeListFrom;
    mapping(address => bool) internal _privilegeListTo;
}
