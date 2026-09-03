// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface ERC721NormalInterface {

    event OwnershipTransferred(address oldManager, address newManager);

    function mint(address account, uint256 tokenId) external returns (uint256);

    function burn(uint256 tokenId) external;

    function exist(uint256 tokenId) view external returns (bool);

    function setUint256Attribute(bytes32 attributeName, uint256 tokenId, uint256 attributeValue) external;

    function setBytes32Attribute(bytes32 attributeName, uint256 tokenId, bytes32 attributeValue) external;

    function setAddressAttribute(bytes32 attributeName, uint256 tokenId, address attributeValue) external;

    function setBytesAttribute(bytes32 attributeName, uint256 tokenId, bytes memory attributeValue) external;

    function getUint256Attribute(bytes32 attributeName, uint256 tokenId) view external returns (uint256);

    function getBytes32Attribute(bytes32 attributeName, uint256 tokenId) view external returns (bytes32);

    function getAddressAttribute(bytes32 attributeName, uint256 tokenId) view external returns (address);

    function getBytesAttribute(bytes32 attributeName, uint256 tokenId) view external returns (bytes memory);

    function managers() view external returns (address[] memory);

    function setManager(address[] memory who, bool[] memory add) external;

    function setAutoCounterBegin(uint256 offset) external;

    function transferOwnership(address newOwner) external;
}
