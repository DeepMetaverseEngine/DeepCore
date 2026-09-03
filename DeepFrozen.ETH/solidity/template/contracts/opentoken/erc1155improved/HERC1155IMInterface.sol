// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/ownable/OwnableInterface.sol";
import "../../nameserver/nameServiceRef/NameServiceRefInterface.sol";
import "../../nameserver/reentrancy/ReentrancyInterface.sol";
import "../erc1155/HERC1155Interface.sol";
import "./HERC1155IMEvent.sol";

interface HERC1155IMInterface is OwnableInterface, NameServiceRefInterface, ReentrancyInterface, HERC1155Interface, HERC1155IMEvent {

    function mintNormal(uint256 id, uint256 amount) external;

    function mintSudo(address to, uint256 id, uint256 amount) external;

    function burnNormal(uint256 id, uint256 amount) external;

    function burnSudo(address from, uint256 id, uint256 amount) external;

    function setAccessControl(address accessControl_) external;

    function setSupport(bool supportTransfer_, bool supportMint_, bool supportBurn_) external;

    function setBlockListFrom(address[] memory from, bool flag) external;

    function setBlockListTo(address[] memory to, bool flag) external;

    function setPrivilegeListFrom(address[] memory from, bool flag) external;

    function setPrivilegeListTo(address[] memory to, bool flag) external;
    //==========

    function support() view external returns (bool supportTransfer, bool supportMint, bool supportBurn);

    function transferTxs() view external returns (uint256);

    function transferAmounts() view external returns (uint256);

    function interactAmounts() view external returns (uint256);

    function isInteracted(address who) view external returns (bool);
}
