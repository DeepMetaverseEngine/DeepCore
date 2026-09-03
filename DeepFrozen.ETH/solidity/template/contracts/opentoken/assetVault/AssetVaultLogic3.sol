// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./AssetVaultLayout.sol";
import "../../nameserver/nameServiceRef/NameServiceRefLogic.sol";
import "../holders/HERC721HolderLogic.sol";
import "../holders/HERC1155HolderLogic.sol";
import "./AssetVaultInterface.sol";

import "../erc20improved/HERC20IMInterface.sol";
import "../erc1155improved/HERC1155IMInterface.sol";
import "../erc721improved/HERC721IMInterface.sol";

import "./AssetVaultType.sol";

abstract contract AssetVaultLogic3 is AssetVaultLayout, NameServiceRefLogic, HERC721HolderLogic, AssetVaultInterface3 {

    using SafeMath for uint256;
    using SafeERC20 for IERC20;

    function _depositErc721(
        bytes32 erc721TokenName,
        address owner,
        uint256 tokenId
    ) internal {
        bytes32[] memory erc721TokenNames = new bytes32[](1);
        erc721TokenNames[0] = erc721TokenName;
        uint256[] memory tokenIds = new uint256[](1);
        tokenIds[0] = tokenId;

        _depositErc721s(erc721TokenNames, owner, tokenIds);
    }

    function _depositErc721s(
        bytes32[] memory erc721TokenNames,
        address owner,
        uint256[] memory tokenIds
    ) internal {
        require(erc721TokenNames.length == tokenIds.length, "deposit Erc721s, length mismatch");

        for (uint256 i = 0; i < erc721TokenNames.length; i++) {

            bytes32 erc721TokenName = erc721TokenNames[i];
            address erc721Token = ns().getSingleSafe(erc721TokenName);

            uint256 tokenId = tokenIds[i];
            require(tokenId != 0, "deposit Erc721s, tokenId is zero");
            require(IERC721(erc721Token).ownerOf(tokenId) == owner, "deposit Erc721s, owner of given token does not belong to 'owner'");

            IERC721(erc721Token).safeTransferFrom(owner, address(this), tokenId);
        }
        emit DepositErc721(owner, erc721TokenNames, tokenIds, owner);
    }

    //map(if token not exists) + sync + withdraw
    function _withdrawErc721(
        address owner,
        AssetVaultType.Erc721Param memory param,
        uint256 traceId
    ) internal {

        AssetVaultType.Erc721Param[] memory params = new AssetVaultType.Erc721Param[](1);
        params[0] = param;
        _withdrawErc721s(owner, params, traceId);
    }

    //use this instead of withdraw
    function _withdrawErc721s(
        address owner,
        AssetVaultType.Erc721Param[] memory params,
        uint256 traceId
    ) internal {
        for (uint256 i = 0; i < params.length; i++) {
            AssetVaultType.Erc721Param memory param = params[i];

            require(param.tokenId != 0, "withdraw Erc721, tokenId should never be zero");

            address erc721Token = ns().getSingleSafe(param.erc721TokenName);

            if (!HERC721IMInterface(erc721Token).exists(param.tokenId)) {

                //map(create) it
                (uint256 from, uint256 to) = HERC721IMInterface(erc721Token).tokenIdMapRange();
                require((from <= param.tokenId && param.tokenId < to) || (from == 0 && to == 0), "withdraw Erc721, tokenId should be in [from,to) of tokenIdMapRange");

                HERC721IMInterface(erc721Token).mintSudo(address(this), param.tokenId);
            }

            require(IERC721(erc721Token).ownerOf(param.tokenId) == address(this), "withdraw Erc721, tokenId does not belong to AssetVault");

            _setAttributeErc721_(
                erc721Token,
                param.tokenId,
                param.attributeNames,
                param.uint256Values,
                param.bytes32Values,
                param.addressValues,
                param.bytesValues
            );

            _withdraw721Hook(param);

            IERC721(erc721Token).safeTransferFrom(address(this), owner, param.tokenId);
        }

        emit WithdrawErc721(
            owner,
            params,
            traceId
        );
    }

    //map(if token not exists) + sync + burn in vault
    function _meltErc721(
        AssetVaultType.Erc721Param memory param
    ) internal {

        require(param.tokenId != 0, "melt Erc721, tokenId should never be zero");

        address erc721Token = ns().getSingleSafe(param.erc721TokenName);

        if (!HERC721IMInterface(erc721Token).exists(param.tokenId)) {

            //map(create) it
            (uint256 from, uint256 to) = HERC721IMInterface(erc721Token).tokenIdMapRange();
            require((from <= param.tokenId && param.tokenId < to) || (from == 0 && to == 0), "melt Erc721, tokenId should be in [from,to) of tokenIdMapRange");

            HERC721IMInterface(erc721Token).mintSudo(address(this), param.tokenId);
        }

        require(IERC721(erc721Token).ownerOf(param.tokenId) == address(this), "melt Erc721, tokenId does not belong to AssetVault");

        _setAttributeErc721_(
            erc721Token,
            param.tokenId,
            param.attributeNames,
            param.uint256Values,
            param.bytes32Values,
            param.addressValues,
            param.bytesValues
        );

        HERC721IMInterface(erc721Token).burnSudo(param.tokenId);

        emit MeltErc721(
            param.erc721TokenName,
            param.tokenId,
            param.attributeNames,
            param.uint256Values,
            param.bytes32Values,
            param.addressValues,
            param.bytesValues
        );
    }


    function _meltErc721s(
        AssetVaultType.Erc721Param[] memory params
    ) internal {
        for (uint256 i = 0; i < params.length; i++) {
            _meltErc721(params[i]);
        }
    }

    //never call this function outside
    function _setAttributeErc721_(
        address erc721Token,
        uint256 tokenId,
        bytes32[] memory attributeNames,
        uint256[] memory uint256Values,
        bytes32[] memory bytes32Values,
        address[] memory addressValues,
        bytes[] memory bytesValues
    ) internal {

        //short cut
        if (attributeNames.length == 0) {
            return;
        }

        require(attributeNames.length == (uint256Values.length + bytes32Values.length + addressValues.length + bytesValues.length), "param length");

        require(tokenId != 0, "setAttribute Erc721, tokenId should never be 0");

        require(HERC721IMInterface(erc721Token).exists(tokenId), "setAttribute Erc721, tokenId should exist");

        HERC721IMInterface(erc721Token).setAttributes(
            attributeNames,
            tokenId,
            uint256Values,
            bytes32Values,
            addressValues,
            bytesValues
        );
    }

    function _withdraw721Hook(
        AssetVaultType.Erc721Param memory param
    ) virtual internal {
        param;
    }
}
