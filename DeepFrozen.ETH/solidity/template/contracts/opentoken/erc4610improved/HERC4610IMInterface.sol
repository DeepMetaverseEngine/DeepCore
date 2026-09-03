// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/ownable/OwnableInterface.sol";
import "../../nameserver/accessControlRef/AccessControlRefInterface.sol";
import "../../nameserver/reentrancy/ReentrancyInterface.sol";
import "../erc4610/HERC4610Interface.sol";
import "./HERC4610IMEvent.sol";

interface HERC4610IMInterface is OwnableInterface, AccessControlRefInterface, ReentrancyInterface, HERC4610Interface, HERC4610IMEvent {

    function mintNormal(uint256 tokenId, uint256 offlineId) external returns (uint256);

    function mintSudo(address to, uint256 tokenId, uint256 offlineId) external returns (uint256);

    function burnNormal(uint256 tokenId) external;

    function burnSudo(uint256 tokenId) external;

    function bind(uint256 tokenId, uint256 offlineId) external;

    function unbind(uint256 tokenId) external;

    function freeze(uint256 tokenId, address unlocker) external;

    function thaw(uint256 tokenId) external;

    function setAccessControl(address accessControl_) external;

    function setSupport(bool supportTransfer_, bool supportMint_, bool supportBurn_) external;

    function setBlockListFrom(address[] memory from, bool flag) external;

    function setBlockListTo(address[] memory to, bool flag) external;

    function setPrivilegeListFrom(address[] memory from, bool flag) external;

    function setPrivilegeListTo(address[] memory to, bool flag) external;
    //==========

    function frozen(uint256 tokenId) view external returns (address);

    function offlineIdToTokenId(uint256 offlineId) view external returns (uint256);

    //zero for unbound or not existence
    function tokenIdToOfflineId(uint256 tokenId) view external returns (uint256);

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
