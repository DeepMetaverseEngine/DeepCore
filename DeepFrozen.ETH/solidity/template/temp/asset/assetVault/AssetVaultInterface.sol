// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../preclude/Preclude.sol";

import "../nameServiceRef/NameServiceRefInterface.sol";

import "./AssetVaultEvent.sol";

interface AssetVaultInterface is NameServiceRefInterface, AssetVaultEvent {

    function mintErc20(
        bytes32 erc20TokenName,
        address owner,
        uint256 amount
    ) external;

    function depositErc20(
        bytes32 erc20TokenName,
        address owner,
        uint256 amount
    ) external;

    function withdrawErc20(
        bytes32 erc20TokenName,
        address owner,
        uint256 amount,
        uint256 traceId
    ) external;

    //==========================================================================================

    function mintErc1155(
        bytes32 erc1155TokenName,
        address owner,
        uint256 tokenId,
        uint256 amount
    ) external;


    function depositErc1155(
        bytes32 erc1155TokenName,
        address owner,
        uint256 tokenId,
        uint256 amount
    ) external;

    function withdrawErc1155(
        bytes32 erc1155TokenName,
        address owner,
        uint256 tokenId,
        uint256 amount,
        uint256 traceId
    ) external;

    //==========================================================================================

    //sync attributes
    function syncErc721(
        bytes32 erc721TokenName,
        uint256 tokenId,
        bytes32[] memory attributeNames,
        uint256[] memory uint256Data,
        bytes32[] memory bytes32Data,
        address[] memory addressData,
        bytes[] memory bytesData
    ) external;

    //mint 721 online to OWNER
    function mintErc721(
        bytes32 erc721TokenName,
        address owner
    ) external returns (uint256 tokenId);

    //mint 721 online to OWNER
    function mintErc721Attributes(
        bytes32 erc721TokenName,
        address owner,
        bytes32[] memory attributeNames,
        uint256[] memory uint256Data,
        bytes32[] memory bytes32Data,
        address[] memory addressData,
        bytes[] memory bytesData
    ) external returns (uint256 tokenId);

    //mint 721 in VAULT
    function mapErc721(
        bytes32 erc721TokenName,
        uint256 tokenId
    ) external;

    function mapErc721Attributes(
        bytes32 erc721TokenName,
        uint256 tokenId,
        bytes32[] memory attributeNames,
        uint256[] memory uint256Data,
        bytes32[] memory bytes32Data,
        address[] memory addressData,
        bytes[] memory bytesData
    ) external;

    function depositErc721(
        bytes32 erc721TokenName,
        address owner,
        uint256 tokenId
    ) external;

    function depositErc721s(
        bytes32 erc721TokenName,
        address owner,
        uint256[] memory tokenId
    ) external;

    /*
    withdraw tokenId from vault
    */
    function withdrawErc721(
        bytes32 erc721TokenName,
        address owner,
        uint256 tokenId
    ) external;

    function withdrawErc721s(
        bytes32 erc721TokenName,
        address owner,
        uint256[] memory tokenId
    ) external;

    //map(if token not exists) + sync + withdraw
    function uploadErc721(
        bytes32 erc721TokenName,
        address owner,
        uint256 tokenId,
        bytes32[] memory attributeNames,
        uint256[] memory uint256Data,
        bytes32[] memory bytes32Data,
        address[] memory addressData,
        bytes[] memory bytesData
    ) external;
}
