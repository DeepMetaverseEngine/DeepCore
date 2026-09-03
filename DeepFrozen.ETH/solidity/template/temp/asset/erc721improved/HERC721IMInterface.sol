// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../preclude/Preclude.sol";

import "../ownable/OwnableInterface.sol";
import "../accessControlRef/AccessControlRefInterface.sol";
import "../reentrancy/ReentrancyInterface.sol";
import "../erc721/HERC721Interface.sol";
import "./HERC721IMEvent.sol";

interface HERC721IMInterface is OwnableInterface, AccessControlRefInterface, ReentrancyInterface, HERC721Interface, HERC721IMEvent {

    function mintNormal(uint256 tokenId) external returns (uint256);

    function mintSudo(address to, uint256 tokenId) external returns (uint256);

    function burnNormal(uint256 tokenId) external;

    function burnSudo(uint256 tokenId) external;

    function setTokenIdMapRange(uint256 begin, uint256 end) external;

    function setTokenIdCounter(uint256 newTokenIdCounter, bool needSub1) external;

    function freeze(uint256 tokenId, address unlocker) external;

    function thaw(uint256 tokenId) external;

    function setUint256Attribute(bytes32 attributeName, uint256 tokenId, uint256 data) external;

    function setBytes32Attribute(bytes32 attributeName, uint256 tokenId, bytes32 data) external;

    function setAddressAttribute(bytes32 attributeName, uint256 tokenId, address data) external;

    function setBytesAttribute(bytes32 attributeName, uint256 tokenId, bytes memory data) external;

    function setAccessControl(address accessControl_) external;

    function setSupport(bool supportTransfer_, bool supportMint_, bool supportBurn_) external;

    function setBlockListFrom(address[] memory from, bool flag) external;

    function setBlockListTo(address[] memory to, bool flag) external;

    function setPrivilegeListFrom(address[] memory from, bool flag) external;

    function setPrivilegeListTo(address[] memory to, bool flag) external;

    //==========

    function tokenIdMapRange() view external returns (uint256, uint256);

    function frozen(uint256 tokenId) view external returns (address);

    function exists(uint256 tokenId) view external returns (bool);

    function getUint256Attribute(bytes32 attributeName, uint256 tokenId) view external returns (uint256);

    function getBytes32Attribute(bytes32 attributeName, uint256 tokenId) view external returns (bytes32);

    function getAddressAttribute(bytes32 attributeName, uint256 tokenId) view external returns (address);

    function getBytesAttribute(bytes32 attributeName, uint256 tokenId) view external returns (bytes memory);

    function support() view external returns (bool supportTransfer, bool supportMint, bool supportBurn);

    function transferTxs() view external returns (uint256);

    function interactAmounts() view external returns (uint256);

    function isInteracted(address who) view external returns (bool);
}
