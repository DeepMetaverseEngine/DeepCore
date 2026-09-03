// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/ownable/OwnableInterface.sol";
import "../../nameserver/nameServiceRef/NameServiceRefInterface.sol";
import "../../nameserver/reentrancy/ReentrancyInterface.sol";
import "../erc721/HERC721Interface.sol";
import "./HERC721IMEvent.sol";

import "./HERC721IMType.sol";

interface HERC721IMInterface is OwnableInterface, NameServiceRefInterface, ReentrancyInterface, HERC721Interface, HERC721IMEvent {

    function mintNormal(uint256 tokenId) external returns (uint256);

    function mintSudo(address to, uint256 tokenId) external returns (uint256);

    function burnNormal(uint256 tokenId) external;

    function burnSudo(uint256 tokenId) external;

    function freezeNormal(uint256 tokenId, address unlocker) external;

    function freezeSudo(uint256 tokenId, address unlocker) external;

    function thawNormal(uint256 tokenId) external;

    function thawSudo(uint256 tokenId, address unlocker) external;

    function setTokenIdMapRange(uint256 begin, uint256 end) external;

    function setTokenIdCounter(uint256 newTokenIdCounter, bool needSub1) external;

    function setUint256Attribute(bytes32 attributeName, uint256 tokenId, uint256 attributeValue) external;

    function setUint256Attributes(bytes32[] memory attributeNames, uint256 tokenId, uint256[] memory attributeValues) external;

    function setBytes32Attribute(bytes32 attributeName, uint256 tokenId, bytes32 attributeValue) external;

    function setBytes32Attributes(bytes32[] memory attributeNames, uint256 tokenId, bytes32[] memory attributeValues) external;

    function setAddressAttribute(bytes32 attributeName, uint256 tokenId, address attributeValue) external;

    function setAddressAttributes(bytes32[] memory attributeNames, uint256 tokenId, address[] memory attributeValues) external;

    function setBytesAttribute(bytes32 attributeName, uint256 tokenId, bytes memory attributeValue) external;

    function setBytesAttributes(bytes32[] memory attributeNames, uint256 tokenId, bytes[] memory attributeValues) external;

    function setAttributes(
        bytes32[] memory attributeNames,
        uint256 tokenId,
        uint256[] memory uint256AttributeValues,
        bytes32[] memory bytes32AttributeValues,
        address[] memory addressAttributeValues,
        bytes[] memory bytesAttributeValues
    ) external;

    function setAccessControl(address accessControl_) external;

    function setSupport(bool supportTransfer_, bool supportMint_, bool supportBurn_) external;

    function setBlockListFrom(address[] memory from, bool flag) external;

    function setBlockListTo(address[] memory to, bool flag) external;

    function setPrivilegeListFrom(address[] memory from, bool flag) external;

    function setPrivilegeListTo(address[] memory to, bool flag) external;

    function setBaseURI(string memory baseURI_) external;

    function setAttributeRegistries(
        HERC721IMType.AttributeRegistry[] memory adds,
        HERC721IMType.AttributeRegistry[] memory removes
    ) external;

    //==========

    function tokenIdMapRange() view external returns (uint256, uint256);

    function frozen(uint256 tokenId) view external returns (uint256);

    function frozenDetail(uint256 tokenId, address unlock) view external returns (uint256);

    function exists(uint256 tokenId) view external returns (bool);

    //warning, gasLimit
    function tokensOfOwner(address owner) view external returns (uint256[] memory);

    function listAttributeRegistries() view external returns (HERC721IMType.AttributeRegistry[] memory);

    function getUint256Attribute(bytes32 attributeName, uint256 tokenId) view external returns (uint256);

    function getUint256Attributes(bytes32[] memory attributeName, uint256 tokenId) view external returns (uint256[] memory);

    function getBytes32Attribute(bytes32 attributeName, uint256 tokenId) view external returns (bytes32);

    function getBytes32Attributes(bytes32[] memory attributeName, uint256 tokenId) view external returns (bytes32[] memory);

    function getAddressAttribute(bytes32 attributeName, uint256 tokenId) view external returns (address);

    function getAddressAttributes(bytes32[] memory attributeName, uint256 tokenId) view external returns (address[] memory);

    function getBytesAttribute(bytes32 attributeName, uint256 tokenId) view external returns (bytes memory);

    function getBytesAttributes(bytes32[] memory attributeName, uint256 tokenId) view external returns (bytes[] memory);

    function support() view external returns (bool supportTransfer, bool supportMint, bool supportBurn);

    function transferTxs() view external returns (uint256);

    function interactAccountsLength() view external returns (uint256);

    function interactAccountsContains(address who) view external returns (bool);

    function interactedAccountsAt(uint256 index) view external returns (address);
}
